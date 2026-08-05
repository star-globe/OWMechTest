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
`GameLayers.cs` の `MyPlayer` / `Unit` などの独自レイヤーに Volume を置くとカメラに拾われない。

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

### 4-4. 残すもの

- 計測表（上記マトリクス）
- Before/After のスクリーンショット（同一フレーム・同一カメラ位置）
- Render Graph Viewer の Before/After キャプチャ
- 「1/8 では破綻した」証拠のスクリーンショット

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
