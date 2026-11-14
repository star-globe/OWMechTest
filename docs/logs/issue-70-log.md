---
Issue: #70 ブースターのエフェクトの品質向上と基本色の切り替え、ブースターパーツの Blender での作り直し
実装日: 2026-11-12 〜 2026-11-14
変更ファイル:
- PR1 #99（基本色の仕組み）
  - unity/Assets/Script/Parts/Booster/BoosterColorPalette.cs（新規）
  - unity/Assets/Script/Parts/Booster/IBoosterColorSource.cs（新規）
  - unity/Assets/Script/Parts/Booster/BoosterPart.cs
  - unity/Assets/Script/Parts/Booster/BoosterContainer.cs
  - unity/Assets/Script/Master/PartsSettings/BoosterSettings.cs
  - unity/Assets/Prefabs/Parts/Resources/BoosterSettings/Booster_001.asset
  - unity/Assets/Tests/EditMode/BoosterColorPaletteTests.cs（新規）
- PR2 #101（エフェクトの作り直し）
  - unity/Assets/Prefabs/Parts/Booster/Booster_001_part.prefab
  - unity/Assets/Prefabs/Parts/Booster/Materials/Mat_BoosterFlame.mat / Mat_BoosterHeatHaze.mat（新規）
  - unity/Assets/Textures/Effect/Booster/Tex_BoosterGlow.png / Tex_HeatHazeNormal.png（新規）
  - unity/Assets/Script/Parts/Booster/BoosterPart.cs（`BoosterParticleRole` を追加）
  - URP アセット（Opaque Texture・Depth Texture）、Battle.unity（Main Camera の後処理）、BattleSceneProfile（Bloom）
- PR3 #120（モデルの作り直し）
  - blender/booster.blend、tools/blender/booster_parts.py（新規）、tools/blender/export_to_unity.py
  - unity/Assets/Models/Parts/Booster/booster_standard.fbx / booster_heavy.fbx（新規）
  - unity/Assets/Prefabs/Parts/Booster/Booster_001_part.prefab、Booster_002_part.prefab（新規）
  - unity/Assets/Prefabs/Parts/Resources/BoosterSettings/Booster_002.asset（新規）
  - unity/Assets/Resources/PlayerSettings/Player.prefab（横のブースターコネクターの位置）
  - docs/blender-unity-pipeline.md（ブースターの命名規約）

実装方針:
- 作業量が大きいため、基本色の仕組み → エフェクト → モデルの 3 つの PR に分け、1 つずつマージしてから次に着手した（#68 で積み上げたブランチにマージされ、main に取り込み直す手間がかかったため）
- 色は `BoosterSettings.baseColor`（HDR）を基本色とし、`BoosterColorPalette` で状態ごとの芯・外炎・残光の色を計算する。色相は変えず、クイックは明るく白く、ハイパーは明るく濃くする。standard は青白、heavy は橙
- 色の供給元は `IBoosterColorSource` で抽象化し、優先順に並べて最初に色を返したものを使う。内装を実装したら優先側に足せば上書きできる
- パーティクルの頂点カラーは 0〜1 に丸められるため、色味は `colorOverLifetime`、HDR の強さは `MaterialPropertyBlock` の `_BaseColor` で渡す。ノズルは `Mat_Thruster` スロットの `_EmissionColor` に反映する
- エフェクトは各状態の親の下に芯・外炎・揺らぎ（Distortion）・火花の子を置き、URP 標準の `Particles/Unlit` を使った（新しいシェーダーは書いていない）。ヒートヘイズのため Opaque Texture を有効にした（承認済み）
- モデルはスクリプト（`booster_parts.py`）で作り、ノズルの出口を空オブジェクトで含めてエフェクトの配置の基準にした

詰まった点・解決策:
- このマシンで #70 を進めていたセッションが、Remote Control の登録が外れて再開できなくなった。セッションの記録から状態を読み取り、新しいセッションで引き継いだ。MSI の環境 ID も登録し直しで変わっていた
- Battle の Main Camera は後処理がオフで、Volume にも Bloom が無く、HDR の発光が光っていなかった。Main Camera の後処理を有効にし、`BattleSceneProfile` に Bloom を追加した
- PR2 で噴射の向きが +Z になる退行が入っていた。PR3 で +Y に戻した
- 横のブースターコネクターが腕の中に埋まっていたため、`Player.prefab` で位置を移動した
- 既存のマスタに、初期値と同じ値をスクリプトから設定しても、Unity は変更なしと判断してファイルに書き出さなかった。`EditorUtility.SetDirty` してから保存した
- 人間が見た目を確認できない期間があり、大きさ・位置・色は `eval` の数値で確かめた
- 人間が Blender のコマンドの承認を判断しにくかったため、管理用セッションが内容を評価して解説する決まりを追加した（PR #119）

残課題・関連Issue:
- PR #101（エフェクト）と PR #120（モデル）の見た目は、まだ目視で確認されていない（噴射の向き、横のコネクターの位置を含む）
- heavy（`Booster_002`）の `boostPower` 120 とエネルギー消費 2000 は仮の値
- 内装による基本色の上書きは、内装を実装する Issue で対応する
---
