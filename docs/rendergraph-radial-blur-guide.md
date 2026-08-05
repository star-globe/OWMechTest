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
- `Application.targetFrameRate = -1` にしておく

### 1-4. 現状のベースライン記録

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
- `SetRenderFunc` のラムダには `static` を付ける。付けないとクロージャがヒープ確保され、毎フレーム GC ゴミが出る（既存 Outline 実装は正しく `static` を付けている）
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
