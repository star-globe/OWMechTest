# Render Graph カスタム Renderer Feature 実装手順書
## ブースト連動ラジアルブラー（Radial / Zoom Blur）

対象: Unity 6000.4.6f1 / URP 17.5.0
参考資料: [Introduction to URP for advanced Unity creators, Unity 6 edition](https://unity.com/ja/resources/introduction-to-urp-advanced-creators-unity-6) — パイプラインコールバックの章（p.109 Render Graph / p.111 Renderer Feature）

---

## 0. このドキュメントの位置づけ

「フル解像度・多タップのブラーをまず動かし（Before）、低解像度化とサンプル数削減で品質を保ったまま GPU 時間を削る（After）」という Before/After を、計測数値つきで残すことを目的とする。

**成果物は3つ。コードだけ作って終わりにしない。**

1. 動作する Renderer Feature（Volume でランタイム制御可能）
2. Before/After の計測表（パス数・GPU ms）
3. 実装中に踏んだ落とし穴のメモ

---

## 1. 事前確認（実装前に必ず）

### 1-1. Render Graph が有効か

`Project Settings > Graphics > Render Graph`（または URP の Rendering セクション）で
**Compatibility Mode (Render Graph disabled) が OFF** であることを確認する。

ON になっていると `RecordRenderGraph` が呼ばれず、旧 `Execute()` 側にフォールバックする。
「コードは書いたのに何も起きない」の最頻出原因。

### 1-2. Render Graph Viewer を開けるようにしておく

`Window > Analysis > Render Graph Viewer`

Unity 6 で追加されたツールで、パスの依存関係・リソースの生存区間・どのパスがカリングされたかが可視化される。
**Before/After の説明資料として Frame Debugger より説得力があるので、最初から使う癖をつける。**

### 1-3. 計測環境の固定

`Project Settings > Quality` を確認する。本プロジェクトは品質レベルによって `vSyncCount` が 0 と 1 で混在している。

- **計測時は vSyncCount = 0 の品質レベルに固定する**（vSync 1 だと 16.6ms に張り付いて差が消える）
- **Game ビューのツールバーにある VSync トグルも OFF にする。** これは QualitySettings とは独立に効くため、Quality を Don't Sync にしてもここが ON だと fps が頭打ちになる
- Android 実機で測る場合は `Player Settings > Android > Optimized Frame Pacing` を OFF にする（フレーム間隔が平滑化されて計測がぶれる）

`Application.targetFrameRate` は Project Settings には存在せず、**ランタイム専用 API なのでコードからしか設定できない**。
本プロジェクトは現状どこでも設定しておらず、デフォルトの `-1`（プラットフォーム標準）のまま。
エディタ/デスクトップでは `-1` = 無制限なので、**エディタ計測では追加設定は不要**。

実機では `-1` が 30fps 相当に解釈されることがあるため、計測用の使い捨てコンポーネントを別途置く。
`InitializeObject` など製品コードには混ぜない。

```csharp
public class BenchmarkSettings : MonoBehaviour
{
    [SerializeField] int _targetFrameRate = 300;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;          // vSync 有効時 targetFrameRate は無視される
        Application.targetFrameRate = _targetFrameRate;
    }
}
```

なお**モバイルはコンポジタ側で vSync が強制されるため、そもそも fps では GPU 負荷を測れない**。
実機の数値は AGI / Xcode の GPU 時間を読む（§4-3）。targetFrameRate の設定は
「30fps に張り付いて差が見えなくなる」のを防ぐためのもの。

### 1-4. ポストプロセスを有効化するか決める（ベースライン記録より前に）

本プロジェクトは**ポストプロセス一式が休眠状態**にある。

- `OWMech_Renderer.asset` の `postProcessData` が null（Post-processing > Enabled = false）
- どのシーンのカメラも `renderPostProcessing` 未設定（Camera 側も OFF）
- `DefaultProfile(URP).asset`（Bloom / Tonemapping / ColorAdjustments 等を設定済み）はどのシーンからも参照されていない

自作 Feature 自体は OFF のままでも動く（`RenderPassEvent` は並び順の指定でしかなく、Volume も
`VolumeManager` から直接読むため）。ただし OFF のままだと以下の問題がある。

1. **中間カラーテクスチャが確保されず、エフェクトが何も出ない。**
   ポストプロセスも他の Renderer Feature も無いと URP はバックバッファへ直接描画するため
   （`m_IntermediateTextureMode: 1` = Auto）、Step 3 の `isActiveTargetBackBuffer` ガードで毎フレーム即 return する。
   **コードは正しいのに表示されない**という、原因の分かりにくい詰まり方をする
2. `BeforeRenderingPostProcessing` を選ぶ理由（ブラー後に Bloom が乗って光が滲む）が成立しない
3. HDR が ON（`m_SupportsHDR: 1`）なのに Tonemapping が無いため、ハイライトが素直にクリップする。
   ラジアルブラーは明部を引き伸ばすエフェクトなのでこの影響を受けやすい

**1 への対策は、ポストプロセスの ON/OFF に関わらず必ず入れる。**

```csharp
public RadialBlurPass(Material material, bool useLegacyGaussian)
{
    _material = material;
    _useLegacyGaussian = useLegacyGaussian;
    requiresIntermediateTexture = true;   // バックバッファ直接描画を抑止
}
```

#### 検討した選択肢

| | 内容 | 判断 |
|---|---|---|
| A | 既存 `DefaultProfile(URP)` を有効化して使う | **不採用**。Bloom / Tonemapping が一気に効いてゲームの見た目が変わる。グラフィック方針の判断が別途必要になる |
| B | ポストプロセスは OFF のまま据え置き、新規プロファイルに Radial Blur のオーバーライドだけ入れる | **採用** |

#### 採用方針: B（ポストプロセスは無効のまま）

**`OWMech_Renderer.asset` も Camera も変更しない。** 理由:

- ポストプロセスを有効にすると、オーバーライドが空でも **UberPost が常時1パス走る**。
  見た目は変わらないのにコストだけ乗るため、「自作 Feature の GPU 時間を素直に読む」という B の狙いと逆行する
- 中間カラーテクスチャは `requiresIntermediateTexture = true` で確保できるので、ポストプロセスに依存する必要がない
- カメラは `Battle.unity` 側にあり（`FollowCamera` が `Camera.main` で動的取得）、
  フィールドシーンには存在しない。カメラ設定を触ると影響範囲が読みにくい

**自作 Volume は `VolumeManager` から直接読むため、ポストプロセス OFF でも動作する。**

なお HDR が ON のまま Tonemapping が無い状態は残るが、これはプロジェクトの既存条件であり
本課題のスコープ外とする。将来 A に切り替える場合は、その時点でベースラインを取り直すこと。

この方針では §1-4 で変更する設定は無いので、そのまま §1-5 へ進む。

### 1-5. 現状のベースライン記録

まだ何も実装していない状態で、以下を記録しておく。これが本当の原点になる。

- Frame Debugger のパス総数
- Profiler の GPU 時間（エディタ実行・実機の両方）

---

## 2. ファイル構成

既存の `Assets/Script/Graphics/OutlineRendererFeature.cs` と同じ階層に置く。
記述スタイル（namespace なし・`_` 始まりの private フィールド・日本語 XML コメント）は既存に合わせる。

```
Assets/Script/Graphics/
  RadialBlurRendererFeature.cs     … Feature + Pass（1ファイルにまとめる。既存 Outline と同じ構成）
  RadialBlurVolumeComponent.cs     … Volume パラメータ定義
  RadialBlurDriver.cs              … CharacterParam のブースト状態 → Volume 値の駆動

Assets/Shader/PostProcess/
  RadialBlur.shader                … Pass 0: Gaussian(Before用) / Pass 1: Radial(After用)
  RadialBlurMaterial.mat
```

シェーダーに Before 用と After 用の2パスを両方残しておくと、
インスペクタのトグル一つで切り替えて計測できるので比較が楽になる。

---

## 3. 実装ステップ

### Step 1 — Volume コンポーネントを先に作る

**Feature より先に Volume を作る。** パラメータの形が決まらないと Pass のインターフェースが決まらないため。

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
[VolumeComponentMenu("OWMech/Radial Blur")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public class RadialBlurVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity   = new(0f, 0f, 1f);   // ブラー強度（0で無効）
    public ClampedIntParameter   sampleCount = new(8, 4, 24);     // タップ数
    public ClampedIntParameter   downsample  = new(2, 0, 3);      // 0=1/1, 1=1/2, 2=1/4, 3=1/8
    public ClampedFloatParameter centerRadius = new(0.2f, 0f, 1f); // 画面中心のブラー除外半径

    public bool IsActive() => intensity.value > 0f;
}
```

**注意点**

- `SupportedOnRenderPipeline` は Unity 6 の属性。旧 `[VolumeComponentMenuForRenderPipeline]` は非推奨なので使わない
- `IPostProcessComponent` から `IsTileCompatible()` は URP 14 以降削除済み。実装するとコンパイルエラー
- `IsActive()` が false のときに Pass 側で即 return することで、**強度0のときパスごと消える** → Render Graph Viewer 上でパスが消えるのが確認できる。これ自体が良いデモになる

#### Step 1 の後: プロファイルと Global Volume を作る

`RadialBlurVolumeComponent` クラスが存在しないと Add Override のメニューに出てこないため、
**プロファイル作成は必ず Step 1 のコンパイルが通ってから行う。** 順序を間違えると「メニューに出ない」で詰まる。

1. `Assets/Scenes/Profiles/` で右クリック → `Create > Rendering > Volume Profile` → `RadialBlurProfile`
2. `Battle.unity` の Hierarchy で右クリック → `Volume > Global Volume` → 名前を `RadialBlurVolume` に
   - **Layer は Default のままにする**（後述）
   - Transform の Scale が (1,1,1) であることを確認（UnityMCP 経由で作ると 0 になることがある）
3. Volume コンポーネント: Mode = Global / Weight = 1 / Priority = 0 / Profile = RadialBlurProfile
4. `Add Override > OWMech > Radial Blur`
5. **各パラメータ左のチェックボックスを ON にする**
6. `intensity` を手で 1.0 にして動作確認 → 問題なければ Step 5 でブースト駆動に差し替え

**5 が最大の落とし穴。** `VolumeComponent` のパラメータは左のチェックを入れないと
「オーバーライドしない」扱いになり、クラス定義側のデフォルト値が使われる。
`intensity` のデフォルトは `0f` なので、チェックを入れ忘れると `IsActive()` が false のまま
パスが積まれず、**コードは正しいのに何も起きない**。動かないと思ったらまずここを疑う。

**Volume Mask の注意:** `UniversalAdditionalCameraData.volumeLayerMask` のデフォルトは
**Default レイヤーのみ**（本プロジェクトのシーンにこの値はシリアライズされておらず、デフォルトのまま）。
UI(5) / Postprocessing(10) / `GameLayers.cs` の独自レイヤーに Volume を置くとカメラに拾われない。
**Canvas の下で作ると UI レイヤーを引き継いでここで詰まる**（§4-7 で実際に踏んだ）。

複数のフィールドシーンで使うことになるので、`Assets/Prefabs/Volume/RadialBlurVolume.prefab`
としてプレハブ化しておくと配置が楽。Step 5 の `RadialBlurDriver` も同じ GameObject に付ける。

### Step 2 — シェーダーを書く

URP の Blit 用ヘッダを使う。`Vert` は自前で書かず Blit.hlsl のものを使う。

```hlsl
Shader "OWMech/PostProcess/RadialBlur"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off Cull Off ZTest Always

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Blit.hlsl"

        float  _Intensity;
        float  _CenterRadius;
        int    _SampleCount;
        float2 _BlurCenter;   // 通常は (0.5, 0.5)
        ENDHLSL

        // Pass 0: Before 用 — フル解像度ガウシアン（2パス分離 or 単純多タップ）
        // Pass 1: After  用 — ラジアルブラー
    }
}
```

**ラジアルブラーの中核**（Pass 1 のフラグメント）

```
uv        … 現在のピクセル
dir       = (_BlurCenter - uv)              // 中心へ向かうベクトル
dist      = length(dir)
falloff   = saturate((dist - _CenterRadius) / (1 - _CenterRadius))
           // 中心付近は素通し、外周ほど強くブラー = ズーム感が出る
step      = dir * _Intensity * falloff / _SampleCount

color = 0
for (i = 0; i < _SampleCount; i++)
    color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + step * i)
color /= _SampleCount
```

**サンプル数を削るときの必須テクニック**

タップ数を 16 → 6 に落とすと縞状のバンディングが出る。
ここで**サンプル開始位置をピクセルごとに微小ランダムでずらす（dither / jitter）**と、
バンディングが視覚的に消えて 6 タップでも 16 タップと見分けがつかなくなる。

```
float noise = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
uv += step * noise;   // ループ前に開始位置をずらす
```

**これがこの企画の一番おいしいところ。**
「タップ数を 1/3 にして、代わりに 1 命令の dither を足したら見分けがつかなくなった」は、
品質とパフォーマンスの両立の実演としてそのまま話せる。Before/After のスクリーンショットを必ず残すこと。

**シェーダー側の注意点**

- `SAMPLE_TEXTURE2D_X` / `_BlitTexture` を使う（`_MainTex` ではない）
- `sampler_LinearClamp` を使う。Repeat だと画面端で反対側の色を拾って破綻する
- **オフセットは必ず `float2` で受ける（実際に踏んだ）。**
  `float step = dir * ...;` のようにスカラーで宣言すると **`.x` に切り詰められ**、
  放射状ではなく斜め方向の一様なブラーになる。エラーも警告も出ないので気づきにくい。
  変数名も `step` は HLSL の組み込み関数と衝突するため `delta` 等にする
- **`_BlurCenter` をシェーダー内定数にするなら `static const` にする（実際に踏んだ）。**
  HLSL ではグローバル変数はデフォルトで uniform（定数バッファ変数）扱いになり、
  `static` を付けない `const` は「書き換え不可の uniform」として**初期化子が無視される**。
  C# 側から設定していないと `(0, 0)` になり、**画面左下を中心にブラーがかかる**。エラーも警告も出ない

  ```hlsl
  static const float2 _BlurCenter = float2(0.5, 0.5);   // 定数にするならこう
  ```

  ただし**推奨は uniform のまま C# から渡すこと**。機体の進行方向やロックオン対象に中心をずらせると
  速度感が大きく変わるため、`PassData` 経由で `SetVector` する形にしておく
  （`_Intensity` 等と同じパターン）。中心を動かせること自体が Volume 化の価値の説明材料になる
- **アスペクト比の補正。** `length(dir)` は UV 空間の距離なので、16:9 では
  ブラー除外領域が真円ではなく横長の楕円になる。気になる場合は X 成分に補正を掛ける

  ```hlsl
  float2 d = dir;
  d.x *= _ScreenParams.x / _ScreenParams.y;
  float dist = length(d);
  ```
- ループ回数を変数にすると展開されない。`[loop]` を明示するか、タップ数を `#pragma multi_compile` でバリアント化する。**後者のほうが速いが、バリアント数が増える。このトレードオフも計測して記録する価値がある**

### Step 3 — Renderer Feature / Pass を書く

既存の `OutlineRendererFeature.cs` が `RecordRenderGraph` のお手本になっている。ただし**あれは往復 Blit をそのまま残しているので、構造はそのままコピーしない**。

#### クラス構造

`RadialBlurRendererFeature.cs` 1ファイルに、**3階層の入れ子**で書く（既存 Outline と同じ構成）。

```
RadialBlurRendererFeature : ScriptableRendererFeature   … public、ファイル名と一致必須
 └ RadialBlurPass : ScriptableRenderPass                … 入れ子。public 不要
    └ PassData                                          … さらに入れ子。パス固有のデータ
```

`ScriptableRendererFeature` だけは Unity がインスペクタで列挙する都合上 public かつファイル名一致が必要だが、
入れ子の Pass / PassData にはその制約がない。`PassData` を Pass の中に閉じ込めるのは、
パスごとに必要なデータが異なるため外に出すと使い回しの誘惑が生まれるから。URP のサンプルもこの形。

```csharp
public class RadialBlurRendererFeature : ScriptableRendererFeature
{
    [SerializeField] Material _material;
    [SerializeField] RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    [SerializeField] bool _useLegacyGaussian = false;   // Before/After 切り替え用

    RadialBlurPass _pass;

    public override void Create()
    {
        _pass = new RadialBlurPass(_material, _useLegacyGaussian) { renderPassEvent = _renderPassEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null) return;
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing) { /* 生成したマテリアルインスタンスがあれば破棄 */ }

    // ↓ Pass はこの中に入れ子で定義する
    class RadialBlurPass : ScriptableRenderPass
    {
        readonly Material _material;
        readonly bool _useLegacyGaussian;

        public RadialBlurPass(Material material, bool useLegacyGaussian)
        {
            _material = material;
            _useLegacyGaussian = useLegacyGaussian;
        }

        class PassData { /* Step 4 参照 */ }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // 次の「RecordRenderGraph の骨子」がここに入る
        }
    }
}
```

**RenderPassEvent は `BeforeRenderingPostProcessing` を選ぶ。** 理由:

- Bloom / Tonemapping の前にブラーがかかるので、ブースト時の光がブラーごと滲んで自然に見える
- `AfterRenderingPostProcessing` にすると、その時点で書き込み先がバックバッファになっているケースがあり、`isActiveTargetBackBuffer` の分岐が必要になって面倒

`RecordRenderGraph` の骨子:

```csharp
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    var resourceData = frameData.Get<UniversalResourceData>();
    var cameraData   = frameData.Get<UniversalCameraData>();

    // 1. カメラ種別でフィルタ（Scene View / Preview にかけない）
    if (cameraData.cameraType != CameraType.Game) return;

    // 2. バックバッファ直書き時は中間テクスチャが使えないので抜ける
    if (resourceData.isActiveTargetBackBuffer) return;

    // 3. Volume から現在値を取得。無効なら何も積まない
    var volume = VolumeManager.instance.stack.GetComponent<RadialBlurVolumeComponent>();
    if (volume == null || !volume.IsActive()) return;

    var source = resourceData.activeColorTexture;

    // 4. 縮小バッファを作る
    var desc = renderGraph.GetTextureDesc(source);
    desc.width  >>= volume.downsample.value;
    desc.height >>= volume.downsample.value;
    desc.name = "RadialBlurTemp";
    desc.clearBuffer = false;
    desc.msaaSamples = MSAASamples.None;
    desc.depthBufferBits = 0;
    desc.filterMode = FilterMode.Bilinear;   // 拡大して戻すので必須
    var temp = renderGraph.CreateTexture(desc);

    // 5. Pass A: カメラカラー → 縮小バッファ（ここでラジアルブラー適用）
    // 6. Pass B: 縮小バッファ → カメラカラー（バイリニア拡大で戻す）
}
```

**Pass B を忘れないこと（実際に踏んだ）。**
`temp` に書き込むだけで誰も消費しないと、**Render Graph はそのパスを丸ごとカリングする**。
`IsActive()` が true でもエフェクトは一切出ない。Render Graph Viewer 上ではパスが消えて見える。

```csharp
using (var builder = renderGraph.AddRasterRenderPass<PassData>("RadialBlurComposite", out var passData))
{
    passData.source = temp;
    builder.UseTexture(passData.source);
    builder.SetRenderAttachment(source, 0);
    builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
        Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, true));
}
```

最後の引数 `true` が bilinear 指定。1/4 解像度から拡大して戻すので、
`false`（point）にするとモザイク状になる。

**パス数について正直に理解しておくこと**

「往復 Blit を削る」と言っても、**同一テクスチャの読み書きはできないため、カメラカラーに書き戻す最終パスは原理的に消せない**。
削れるのは中間の余計な往復であって、最低 2 パス（縮小ブラー → 拡大合成）は必要。
ここを「1パスにできます」と言うと面接で突っ込まれる。**「2パスが下限で、そのうち1パス目を 1/16 の面積にした」が正しい主張。**

削減できたのは**パス数ではなくピクセル処理量**であり、そこを数字で示すのがこの課題の本質。

### Step 4 — Volume の値を Pass に渡す（最大の落とし穴）

**やってはいけない書き方:**

```csharp
// NG: RecordRenderGraph の時点で Material に直接書く
_material.SetFloat("_Intensity", volume.intensity.value);
```

Render Graph は「記録」と「実行」が分離している。マテリアルはアセットへの参照なので、
**同じマテリアルを複数パスで使い回すと、実行時には全パスが最後に書いた値を読む**。
1パスだけなら偶然動くが、後からパスを増やした瞬間に壊れる。原因が非常に分かりにくい。

**正しい書き方:** 値を `PassData` に入れ、`SetRenderFunc` の中（=実行時）で設定する。

```csharp
class PassData
{
    public TextureHandle source;
    public Material material;
    public float intensity;
    public int sampleCount;
    public float centerRadius;
}

using (var builder = renderGraph.AddRasterRenderPass<PassData>("RadialBlur", out var passData))
{
    passData.source       = source;
    passData.material     = _material;
    passData.intensity    = volume.intensity.value;
    passData.sampleCount  = volume.sampleCount.value;
    passData.centerRadius = volume.centerRadius.value;

    builder.UseTexture(passData.source);
    builder.SetRenderAttachment(temp, 0);

    builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
    {
        data.material.SetFloat("_Intensity", data.intensity);
        data.material.SetInt("_SampleCount", data.sampleCount);
        data.material.SetFloat("_CenterRadius", data.centerRadius);
        Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
    });
}
```

- シェーダープロパティ名の文字列は `static readonly int` に `Shader.PropertyToID` でキャッシュする
- **`SetRenderFunc` のラムダには `static` を付ける（C# 9）。** 理由は2つあり、後者が本質:
  - 何もキャプチャしなければ delegate が static フィールドにキャッシュされ、毎フレームのヒープ確保が消える
  - **Pass のフィールドを参照すると `this` が暗黙にキャプチャされ、実行時に「記録時の値」ではなく「最新の値」を読んでしまう。**
    `_pass` は Game ビューと Scene ビューで使い回されるため、Game の記録 → Scene の記録（フィールド上書き）→ 両方を実行、
    という順序で Game 側が Scene の値を読む。Scene ビューを閉じると再現しなくなる厄介なバグになる。
    `static` を付けるとこの書き方がコンパイルエラーになり、「渡していいのは `PassData` と `ctx` だけ」を言語機能で強制できる
  - 既存 `OutlineRendererFeature.cs` も正しく `static` を付けている
- **`ctx.cmd.SetGlobalXXX` など「グローバル状態」を変更する場合は `builder.AllowGlobalStateModification(true)` が必須。** これを忘れると実行時エラーになる

### Step 5 — ブースト状態と接続する

`CharacterParam` に既にフラグが揃っている（`IsBoost` / `IsQuickBoost` / `IsHyperBoost` / `IsJumpBoost`）。
これを Volume の `intensity` に流し込む MonoBehaviour を作る。

```csharp
// シーンに Global Volume を1つ置き、そこにこのコンポーネントを付ける
[SerializeField] Volume _volume;
RadialBlurVolumeComponent _radialBlur;

void Start()
{
    // sharedProfile ではなく profile を使う（後述）
    _volume.profile.TryGet(out _radialBlur);
}

void Update()
{
    float target = param switch
    {
        { IsHyperBoost: true } => 1.0f,
        { IsQuickBoost: true } => 0.6f,
        { IsBoost:      true } => 0.3f,
        _                      => 0f,
    };
    // 立ち上がりは速く、戻りは緩やかにすると気持ちいい
    _current = Mathf.MoveTowards(_current, target, (target > _current ? _riseSpeed : _fallSpeed) * Time.deltaTime);
    _radialBlur.intensity.value = _current;
}
```

**落とし穴:** `volume.sharedProfile` を書き換えると **Volume Profile アセット本体が書き換わり、エディタで実行するたびに .asset に差分が出る**。
`volume.profile` は初回アクセス時にランタイムクローンを返すので、必ずこちらを使う。

**このステップまでやり切ることに意味がある。**
スライダーを手で動かすデモで止めず、ゲームプレイと接続することで
「ポストエフェクトフレームワークを理解して使った」証明になる。

### Step 6 — Renderer アセットに登録する

`Assets/Settings/OWMech_Renderer.asset` の `m_RendererFeatures` は現在**空**。
インスペクタから `Add Renderer Feature > Radial Blur Renderer Feature` で追加し、マテリアルを割り当てる。

（同じ理由で、既存の `OutlineRendererFeature` も未登録＝現状1フレームも実行されていない。後述）

---

## 4. 計測プロトコル

**コードが動いたら終わりではなく、ここからが本体。**

### 4-1. 測る条件を固定する

| 項目 | 固定値 |
|---|---|
| シーン | Field_000_1 など1つに固定 |
| カメラ位置 | 固定（可能ならタイムラインかスクリプトで再現可能に） |
| 解像度 | 1920×1080 固定（実機は実解像度を記録） |
| vSync | OFF |
| ブースト状態 | intensity = 1.0 に固定して測る |

### 4-2. 計測マトリクス

```
                     GPU ms      パス数    見た目の破綻
Before: 1/1, 16タップ, ガウシアン
After1: 1/1,  8タップ, ラジアル
After2: 1/2,  8タップ, ラジアル
After3: 1/4,  8タップ, ラジアル
After4: 1/4,  6タップ, ラジアル + dither   ← 本命
After5: 1/8,  6タップ, ラジアル + dither   ← 破綻する側の限界を示す
```

**破綻する条件（After5）まで測って載せること。**
「1/4 が最適」ではなく「1/8 では輪郭が溶けたので 1/4 で止めた」と言えると、
閾値を探した過程を示せる。上限だけ載せると「たまたま動いた」に見える。

### 4-3. ツール別の使い分け

| ツール | 何を取るか |
|---|---|
| Render Graph Viewer | パスの依存関係、リソース生存区間、カリングされたパス。**Before/After のスクショはこれが一番分かりやすい** |
| Frame Debugger | パス総数、描画順、実際の解像度の確認 |
| Unity Profiler (GPU) | エディタ上の GPU 時間。あくまで参考値 |
| Android GPU Inspector / RenderDoc | 実機のボトルネック内訳。**数値として信用できるのはこちら** |
| Xcode GPU Frame Capture | iOS 実機。同上 |

本プロジェクトは Android ARMv7+ARM64 ビルド設定済みなので、実機計測まで到達できる。
**エディタ数値だけだと「フィルレート律速を改善した」という主張の裏付けが弱い。実機の ms を必ず取る。**

### 4-4. パラメータを振っても差が出ないとき（実際に踏んだ）

デスクトップ 1080p の本プロジェクトで実測したところ、downsample / sampleCount を変えても
GPU 時間に有意な差が出なかった。**計測が成立していない状態だったため。**

| | FPS | Global | GPU | DrawCalls | SetPass |
|---|---|---|---|---|---|
| ブラー OFF | 112.2 | 8.9 ms | **1.0 ms** | 100 | 23 |
| ブースト中（弱） | 118.2 | 8.5 ms | **0.5 ms** | 102 | 25 |
| ブースト中（強） | 59.3 | 16.9 ms | **1.1 ms** | 102 | 25 |

**ブラーを掛けたほうが GPU 時間が短い（0.5 < 1.0）という結果が出ている時点で、
測定値がノイズに埋もれている。** この状態でパラメータを振っても何も分からない。

一方 `DrawCalls 100 → 102` / `SetPass 23 → 25` の +2 は、追加した2パスが走っている証拠。実装自体は正しい。

#### 原因: GPU バウンドではない

Global Frametime 8.5〜16.9ms に対し GPU は 1ms 前後、CPU Main Thread が 78% を占める。
GPU はフレームの大半を遊んでいる。

| 条件 | テクスチャフェッチ数 |
|---|---|
| 1/1 解像度・16タップ | 1920×1080×16 ≈ **3300万** |
| 1/4 解像度・8タップ | 480×270×8 ≈ **100万** |

デスクトップ GPU なら前者でも 0.3ms 程度、後者は 0.01ms 未満。
**差の絶対値が Statistics パネルの表示分解能（0.1ms 刻み）とフレーム間のばらつきより小さい。**

「低解像度化で GPU 時間を削る」はフィルレート律速の環境で初めて意味を持つ。
デスクトップ 1080p のこの規模のシーンには、そもそも削る余地がない。

#### 先に確認すること: パラメータが届いているか

- **downsample**: Frame Debugger で `RadialBlur` パスのレンダーターゲット解像度が
  `480×270` / `960×540` と変わるか
- **sampleCount**: 4 と 24 で**見た目（縞状のバンディング）が変わるか**。
  変わらないなら uniform が届いていない

sampleCount が効かない場合はシェーダーの型宣言を疑う。

```hlsl
int _SampleCount;      // Unity の SetInt は float として書き込むため値が化ける可能性
float _SampleCount;    // float 宣言 + SetFloat が確実（Unity 6 では SetInteger も可）
```

#### 差を出すための条件作り

意図的にフィルレート律速へ持ち込む。

| 手段 | 効果 |
|---|---|
| Render Scale を 2.0 に（または Game ビューを 4K） | ピクセル数 4倍 |
| タップ数を 8 → 64 / 128 | 演算量 8〜16倍 |
| Before を本気で重く（フル解像度 32タップ ガウシアン） | 数 ms のオーダーに乗る |
| **Android 実機** | モバイル GPU はフィルレート律速。**本来の土俵** |

デスクトップなら **4K × 64タップ × フル解像度**から始め、そこから解像度とタップ数を落として
削減幅を見る形にすると §4-2 のマトリクスが機能する。

計測は Statistics パネルではなく **Profiler の GPU モジュール**で行う。
フレーム全体 1ms の中の 0.1ms を全体値の差分で読もうとするのは無理筋。

#### 「差が出なかった」も報告すべき結果

失敗ではない。**最適化の効果を主張する前に律速要因を特定できている**ことの証明になる。

> 1/4 解像度化を実装したが、デスクトップ 1080p・GPU 1ms のシーンでは差が測定限界以下だった。
> CPU バウンドだったため。フィルレート律速の条件（4K / 高タップ数 / モバイル実機）を作って
> 初めて効果が測定できた。

「1/4 にしたら 4 倍速くなりました」より実務的で、突っ込まれにくい。
数字が出なかった条件のスクリーンショットも捨てずに残す。

### 4-5. 実測結果と考察（デスクトップ）

§4-4 の通り負荷条件を作った結果、Profiler の GPU モジュールで有意差が取れた。

**計測条件:** 4K (3840×2160) / **RenderScale 2.0** / GPU Usage の Median 値

| 条件 | GPU ms | ブラーの寄与 |
|---|---|---|
| ブラー無し | **3.41** | — |
| ds0 (1/1)・4タップ | **3.85** | 0.44 ms |
| ds0 (1/1)・48タップ | **11.52** | 8.11 ms |

**タップ数 12 倍でコスト 18.4 倍。両軸とも効く。**

> **注意: RenderScale 未設定時の旧データ（ベースライン 6.0ms 系）とは条件が異なるため統合しないこと。**
> 計測条件を変えたら表は差し替える。
> また GPU 時間はスパイクが混ざるため、平均ではなく **Median** を取る。

#### 中間バッファのフォーマット（実測で確定済み）

`OWMech_URPAsset.asset` の設定:

```yaml
m_SupportsHDR: 1
m_HDRColorBufferPrecision: 0    # 0 = _32Bits（デフォルト）
m_AllowPostProcessAlphaOutput: 0
```

URP は HDR 有効時、`HDRColorBufferPrecision` が `_32Bits` かつアルファ出力が不要なら
**`B10G11R11_UFloatPack32`（4 バイト/px、アルファ無し）**を選ぶ。
`R16G16B16A16_SFloat`（8 バイト/px）になるのは `_64Bits` を選んだ場合か、B10G11R11 非対応環境。

実測ログでも確認済み: `format=B10G11R11_UFloatPack32 size=3840x2160`

**アルファは最初から存在せず、「中間バッファを R11G11B10 に落とす」最適化は既に適用済み。
ここに削る余地はない。** 感度を見たい場合は逆に `_64Bits`（8B/px）にして倍増するか確認する。

```csharp
var desc = renderGraph.GetTextureDesc(source);
Debug.Log($"format={desc.format} size={desc.width}x{desc.height}");
```

#### ボトルネックの解釈

このシェーダーは `step = span / _SampleCount` なので、
**タップ数を変えてもブラーの総距離は変わらない。** 4タップは疎に、48タップは密に、同じ範囲をサンプリングする。
つまり触るテクセルの範囲は同じで、**メモリ traffic はほぼ変わらず、フェッチ命令数だけが 12 倍**になる。

したがって帯域律速ではなく **TEX ユニットのスループット律速**。
コストは**総フェッチ数（ピクセル数 × タップ数）**に比例する。

#### 計測時の落とし穴: スイープする軸以外を固定する

`downsample` のデフォルトは `2`（= 1/4）。
**1/4 のままサンプル数を 4→48 に振ると差が 1ms 程度にしか出ず、
「サンプル数は効かない」という誤った結論に至る。** 実際に一度この誤りを踏んだ。

ds0（フル解像度）で測り直したところ差は **7.67ms** あった。
**片方の軸を低い値に固定したまま他方を振ると、影響を過小評価する。**

#### 計測マトリクス（確定版）

**計測条件:** 4K (3840×2160) / RenderScale 2.0 / GPU Usage の Median 値 /
カメラ位置固定（カメラ位置で数値が動くため、ベースラインから通しで測り直すこと）

測定値（GPU ms）。**ブラー無しのベースラインは 2.67ms**:

| | 4タップ | 16タップ | 48タップ |
|---|---|---|---|
| ds0 (1/1) | 4.08 | 6.40 | 12.55 |
| ds1 (1/2) | 3.56 | 4.56 | 6.50 |
| ds2 (1/4) | 3.40 | 3.87 | 4.46 |
| ds3 (1/8) | 3.40 | 3.53 | 4.13 |

ベースライン 2.67ms を引いた**ブラーの正味コスト**:

| | 4タップ | 16タップ | 48タップ |
|---|---|---|---|
| **ds0 (1/1)** | 1.41 | 3.73 | **9.88** |
| **ds1 (1/2)** | 0.89 | 1.89 | 3.83 |
| **ds2 (1/4)** | 0.73 | 1.20 | 1.79 |
| **ds3 (1/8)** | 0.73 | 0.86 | 1.46 |

#### 発見1: フル解像度では完全に線形

```
コスト = 0.65 + 0.192 × タップ数
```

| タップ数 | 予測 | 実測 |
|---|---|---|
| 4 | 1.42 | 1.41 |
| 16 | 3.72 | 3.73 |
| 48 | 9.87 | 9.88 |

**3点とも 0.01ms 以内で一致。** フル解像度では純粋にフェッチ数律速であることが確認できた。

#### 発見2: 固定コスト 0.65ms の正体は合成パス

線形フィットの切片 0.65ms が、**タップ数にも解像度にも依存しない固定コスト**。
これは**フル解像度でカメラカラーに書き戻す合成パス**そのもの。
ds2 / ds3 の 4タップがどちらも 0.73ms で、ほぼ切片に張り付いている。

§3 Step 3 で述べた「**パス数は 2 が下限**」が、ここで **0.65ms という具体的な数値**として現れた。
理屈で言っていた下限が実測で確認できた。

#### 発見3: 1/4 より下げても効果がない

タップあたりの限界コストを ds ごとに算出（48タップと16タップの差 ÷ 32）:

| ds | タップあたり ms | ds0 比 | 理論値（ピクセル比） |
|---|---|---|---|
| 0 (1/1) | 0.192 | 1.00 | 1.00 |
| 1 (1/2) | 0.061 | 0.32 | 0.25 |
| 2 (1/4) | 0.0184 | 0.096 | 0.063 |
| 3 (1/8) | **0.0188** | **0.098** | 0.016 |

**ds2 と ds3 がほぼ同じ。1/4 から先は下げても改善しない。**

理由: **ブラーパスは出力解像度に関わらず、常にフル解像度のソーステクスチャを読む。**
出力解像度を下げると隣接する出力ピクセルがソース上で離れた位置を読むため、
**テクスチャキャッシュのヒット率が落ちる。** フェッチ数は減るが 1 フェッチあたりが高くなり、
途中で相殺されて頭打ちになる。

「解像度を下げれば下げるほど速い」という素朴な予想を、実測が明確に否定している。

#### 結論: 最適点は ds2（1/4）

| | 設定 | GPU 時間 | ブラー正味 |
|---|---|---|---|
| Before | ds0・48タップ | 12.55 ms | 9.88 ms |
| After | ds2・16タップ | **3.87 ms** | **1.20 ms** |

**ブラーのコストを 88% 削減。フレーム全体では 12.55ms → 3.87ms（69% 削減）。**

ds3 を選ばない理由も数字で言える。ds2 → ds3 は 48タップでも **0.33ms しか減らず、画質だけ落ちる**。
「1/8 では輪郭が溶けたので 1/4 で止めた」に加えて
「**そもそも性能上のメリットもほぼ無かった**」と言える。

残る確認は画質のみ。**ds2 × 16タップ + dither が ds2 × 48タップと見分けがつくか**を目視で確認する。
つかなければ 16タップが最終回答、つくなら 24タップあたりに戻す。

#### 結論の書き方

> コストは総フェッチ数（ピクセル数 × タップ数）に比例する。**両軸とも効く。**
> ただし解像度は 1/4 で頭打ちになる（ソースは常にフル解像度で読むためキャッシュ効率が落ちる）。
> また合成パスがフル解像度である以上 0.65ms の下限が存在する。
> 最適点は 1/4 解像度 × 16タップで、ブラーのコストを 88% 削減できた。

「サンプル数は効かない」と書かないこと。反例が自分の計測データに存在する状態になる。
「**効くが、削減効率が違う**」が実測に即した表現。

### 4-6. 残すもの

- 計測表（上記マトリクス）
- Before/After のスクリーンショット（同一フレーム・同一カメラ位置）
- Render Graph Viewer の Before/After キャプチャ
- 「1/8 では破綻した」証拠のスクリーンショット

---

## 4-7. トラブルシュート: `IsActive()` が false のまま

エフェクトが出ないときの最頻出パターン。まず `RecordRenderGraph` の先頭に一時的に入れて切り分ける
（毎フレーム出るので Console の Collapse を ON にする）。

```csharp
var stack = VolumeManager.instance.stack;
var vc = stack.GetComponent<RadialBlurVolumeComponent>();
Debug.Log($"comp={vc != null} value={vc?.intensity.value} override={vc?.intensity.overrideState}");
```

| ログ | 意味 |
|---|---|
| `override=False` | プロファイルのオーバーライドがブレンドされていない（下記1） |
| `override=True, value=0` | Volume は拾われている。値を 0 で上書きしている犯人がいる（下記2） |
| `comp=False` | VolumeManager にコンポーネントが登録されていない（稀） |
| `value>0` | `IsActive()` は true のはず。別の箇所の問題 |

### 1. `overrideState` が false のまま（本命）

`ClampedFloatParameter` のコンストラクタは `overrideState` のデフォルトが **false**。
プロファイルでパラメータ左のチェックを入れない限り、その値はブレンド時に無視され、
スタックにはクラス定義側の初期値 0 が残る。

**重要: `intensity.value` を書き換えても、`overrideState` が false なら一切反映されない。**
チェックボックスを手で入れるより、ドライバ側で明示的に立てるほうが確実
（プロファイルアセットを作り直しても壊れない）。

```csharp
void Start()
{
    _volume.profile.TryGet(out _radialBlur);
    _radialBlur.intensity.overrideState    = true;   // ← 必須
    _radialBlur.sampleCount.overrideState  = true;
    _radialBlur.downsample.overrideState   = true;
    _radialBlur.centerRadius.overrideState = true;
}
```

### 2. `RadialBlurDriver` が毎フレーム 0 で上書きしている

ドライバはブースト非入力時に `target = 0f` を毎フレーム書き込む。
**インスペクタで手動で Intensity を上げても、次の Update で 0 に戻される。**
手動テスト中はドライバのコンポーネントを無効化すること。

### 3. `volume.profile` のクローンと、編集しているアセットが別物

`_volume.profile`（`sharedProfile` ではない）に初回アクセスした時点で Unity はクローンを生成し
Volume に差し替える。**Play 中に Project ウィンドウで `RadialBlurProfile.asset` を編集しても、
走っているクローンには反映されない。**
Play 中は Hierarchy の Volume を選択し、そこに表示されるプロファイル（`(Clone)` 付き）を編集する。

### 4. Volume の GameObject が Volume Mask 外のレイヤーにある（実際の原因）

**本プロジェクトで実際に起きたのはこれ。** `PostProcess.prefab` 内の `Global Volume` が
`m_Layer: 5`（UI レイヤー）にあり、カメラに拾われていなかった。

`Battle.unity` の Main Camera には `UniversalAdditionalCameraData` が付いていないため、
URP は **`volumeLayerMask` = Default (layer 0) のみ** にフォールバックする。
UI レイヤーの Volume は評価対象外となり、スタックには既定プロファイルの値だけが残って
`override=False, value=0` になる。プロファイル側の設定（`intensity: m_Value: 1`）は正しくても効かない。

**原因が Canvas 由来なことが多い。** Volume を Canvas の下や UI オブジェクトとして作ると、
RectTransform と UI レイヤーを引き継いでしまう。Volume は UI 要素ではないので
Canvas の外で作り、通常の `Transform` にすること。

#### 本プロジェクトのレイヤー定義

| # | レイヤー |
|---|---|
| 0 | **Default** |
| 5 | UI |
| 10 | **Postprocessing** |
| 11 | MyPlayer |

レイヤー 10 に `Postprocessing` が用意されているので、本来はそこに置くのが設計意図に沿う。
ただしその場合は **Main Camera の Volume Mask に `Postprocessing` を追加する**必要がある
（カメラを選択すると URP が `UniversalAdditionalCameraData` を自動追加する）。
まず Default で動作確認し、後から整理するのが安全。

#### その他の確認項目

- Volume コンポーネントの **Weight が 1** か、**Mode が Global** か
- GameObject と Volume コンポーネントが**有効**か
- Volume を置いたシーンが実際にロードされているか
- `RadialBlurDriver` の `reflectBoost` が false だと `Update()` が即 return する（手動テスト時は意図通り）

**切り分けの近道:** `Assets/DefaultVolumeProfile.asset`（既定ボリュームプロファイル）の
RadialBlur > Intensity を 0.5 にすると、シーンの Volume を経由せずに `IsActive()` を true にできる。
これで true になるならレイヤー/マスク側、ならなければコード側の問題と切り分けられる。

**調査時の注意:** シーン YAML はコンポーネントを GUID で参照し、
**プレハブインスタンスは差分しか持たない**。クラス名や `sharedProfile` でシーンファイルを grep しても
プレハブ内の Volume は見つからないので、プレハブ側を直接確認すること。

---

## 5. 落とし穴チェックリスト

実装中に踏んだものにチェックを入れて、面接での失敗談ネタとして残す。

- [ ] Compatibility Mode が ON で `RecordRenderGraph` が呼ばれない
- [ ] 同一テクスチャの読み書きで真っ黒 / エラー → temp 経由が必須
- [ ] `RecordRenderGraph` 内でマテリアルに直接 SetFloat → 複数パスで最後の値に上書きされる
- [ ] `SetRenderFunc` のラムダに `static` がなく毎フレーム GC 発生
- [ ] `AllowGlobalStateModification(true)` 忘れで実行時エラー
- [ ] Scene View にもエフェクトがかかって作業できない → `cameraData.cameraType` でフィルタ
- [ ] 縮小バッファの `filterMode` が Point のままでモザイク状に戻る
- [ ] `sampler_LinearClamp` ではなく Repeat を使い画面端が破綻
- [ ] 出力が使われずパスごとカリングされて「何も起きない」（Render Graph Viewer で確認できる）
- [ ] `volume.sharedProfile` を書き換えて .asset に差分が出る
- [ ] Volume の GameObject が Volume Mask 外のレイヤー（UI 等）にあり、カメラに拾われない（§4-7）
- [ ] 書き戻しパスが無く、出力を誰も消費しないためパスごとカリングされる（§3 Step 3）
- [ ] シェーダーでオフセットをスカラーで宣言し `.x` に切り詰められる（§3 Step 2）
- [ ] グローバル `const` の初期化子が無視され、定数が 0 になる（`static const` が必要）（§3 Step 2）
- [ ] `overrideState` が false で、`intensity.value` を書き換えても反映されない（§4-7）
- [ ] ドライバがブースト非入力時に毎フレーム 0 で上書きし、手動テストができない（§4-7）
- [ ] Play 中に Project ウィンドウのプロファイルアセットを編集し、クローン側に反映されない（§4-7）
- [ ] タップ数削減でバンディング発生 → dither で解決
- [ ] RTHandle を自前で `Alloc` / `Release` しようとする → **Render Graph では不要**。ここは旧 API の知識が邪魔をするポイント

---

## 6. 余力があれば: 既存アウトラインの最適化を第2の事例にする

`Assets/Script/Graphics/OutlineRendererFeature.cs` は既に Render Graph 対応で書かれているが、以下の課題がある。

- **Renderer アセットに未登録**（`m_RendererFeatures: []`）＝ 現状1フレームも実行されていない
- Volume 非対応で、常時フルコストで走る作りになっている
- `cameraType` によるフィルタがなく、Scene View / Preview にもかかる
- 強度0でもパスが積まれる（カリング頼み）

**新規実装より「既存コードの問題を見つけて直した」ほうが実務らしい話になる**ので、
ラジアルブラーが完成した後、同じ PR か次の PR で手を入れる価値がある。
ただし**アウトラインは高周波（1px のエッジ）なので低解像度化には向かない**。
こちらで示すのは「Volume 対応による条件付き実行」と「不要カメラでの除外」であって、解像度削減ではない。

---

## 7. 進め方の目安

| 段階 | 内容 |
|---|---|
| 1 | 事前確認（§1）とベースライン記録 |
| 2 | Volume コンポーネント + シェーダー（Before 用ガウシアンのみ） |
| 3 | Feature / Pass をフル解像度で動かす → **Before 計測** |
| 4 | ラジアル化・低解像度化・dither → **After 計測マトリクス** |
| 5 | ブースト接続（§3 Step 5） |
| 6 | 実機計測・スクリーンショット整理 |
| 7 | （余力）既存アウトラインの最適化 |

段階3の「Before 計測」を飛ばさないこと。**先に最適化版を作ってしまうと Before の数字が永遠に取れなくなる。**

---

## 8. 補足: UI にポストエフェクトを掛けたい場合

本プロジェクトの Canvas は `UICanvas.prefab` を含め**すべて Screen Space - Overlay**（`m_RenderMode: 0`）。
そのため今回のエフェクトは UI に掛かっていない。

### Overlay の UI には Renderer Feature から原理的に触れない

Overlay Canvas は **SRP のカメラループが終わった後**に Unity の UI システムがバックバッファへ直接描画する。
`RenderPassEvent` は SRP のカメラループ内での順序指定なので、最後の `AfterRendering`(1000) を指定しても
Overlay UI より手前になる。設定で回避できる話ではなく、描画経路が違う。

### 掛けたい場合の選択肢

| | 方法 | 備考 |
|---|---|---|
| 1 | Canvas を **Screen Space - Camera** に変更 | 最も簡単。UI が Transparent キューに入り SRP 管理下になるため、`BeforeRenderingPostProcessing`(550) の現行 Feature が**コード変更なしで**UI にも掛かる。Render Camera と Plane Distance の設定が必要 |
| 2 | **Camera Stacking**（Base + Overlay カメラ） | UI だけ別扱いにするなど細かい制御ができる。カメラ管理のコストが増える |
| 3 | **RenderTexture 経由** | 最も柔軟。常時 RT 1枚分のメモリと Blit コストが乗る |

1 を選ぶ場合の注意:
- UI が Transparent キューに入るため、他の半透明オブジェクトとのソート順が変わる
- Plane Distance が近すぎると他オブジェクトが UI を貫通する
- **`UICanvas.prefab` を変更すると Briefing / SelectMenu / License / Result すべてに波及する。**
  Battle だけ変えたいなら Canvas を分ける

### 本題材では UI をぼかさないのが正解

ブースト中にロックオンレティクルや残弾表示がぼけるとゲームとして成立しない。
**UI が鮮明なままなのは結果的に正しい挙動**であり、
「HUD の可読性を優先して Overlay のままにした。掛けたければ Screen Space - Camera に変えれば
同じ Feature がそのまま効く」と仕様上の判断として説明できる状態にしておく。

### 「UI にポストエフェクト」の実需は別物であることが多い

実務で必要になるのは、ポーズ画面やメニューで**背景をぼかしてモーダルを浮かせる**フロストガラス表現。
これは Renderer Feature ではなく別の作りが適切。

- シーンを RenderTexture に描く（または Opaque Texture を使う）
- それをブラーしてパネルの背景 `Image` に貼る
- UI 自体は鮮明なまま

本プロジェクトでは Briefing / SelectMenu が該当。
今回のブラーシェーダーとダウンサンプルのノウハウはそのまま流用できるため、次の題材として自然につながる。
