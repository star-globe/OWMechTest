---
Issue: #229 パーツのマテリアルを Main・Sub・Light の 3 種類に揃え、各メッシュに割り当てる
実装日: 2026-11-28
変更ファイル:
- blender/mech_frame.blend, booster.blend, weapon.blend, environment.blend（マテリアル名の変更）
- tools/blender/mech_frame_parts.py, booster_parts.py, weapon_parts.py, environment_parts.py, export_to_unity.py
- unity/Assets/Models/Parts/**/*.fbx(.meta), Models/Weapons/**/*.fbx(.meta), Models/Environment/**/*.fbx（再エクスポートと Remap）
- unity/Assets/Prefabs/Parts/Materials/Mat_Main.mat, Mat_Sub.mat, Mat_Light.mat, Mat_Thruster.mat（新規）
- unity/Assets/Script/Parts/PartMaterialSlots.cs（新規）, Parts/Booster/BoosterPart.cs
- unity/Assets/Tests/EditMode/PartMaterialSlotsTests.cs（新規）
- docs/blender-unity-pipeline.md, docs/spec/アートディレクション.md, unity/Assets/Models/FBXImportSettings.md

実装方針:
- スロット名を Mat_Body→Mat_Main、Mat_Detail→Mat_Sub、Mat_Visor→Mat_Light に変えた。Mat_Thruster はブースター専用で残す
- 対象は機体フレーム・ブースター・武装（弾を含む）。環境モデルも名前だけ揃え、マテリアルは FBX に埋め込んだまま使う
- Light は頭部のバイザー・カメラアイと武装のセンサーだけ。光らせる箇所が無いパーツには付けない
- .blend はスクリプトで作り直さず、マテリアルのデータブロックの名前だけを変えて再エクスポートした（手で直した部分があっても残るため）
- 既定の .mat は今の FBX に埋め込まれたマテリアル（URP/Lit）を複製して作ったので、見た目は変わらない。FBX のインポート設定の Remap で割り当てる
- #230 向けに、名前の先頭で種類を返す PartMaterialSlots.GetKind を追加し、BoosterPart もこれを使う
- GraphicsStyleSwitcher はどこにも配置されていないので対象外

詰まった点・解決策:
- 承認した方針ではシェーダーを StylizedRealLit にする予定だったが、今のパーツは URP/Lit で表示されていて、StylizedRealLit には DepthOnly/DepthNormals のパスと _EMISSION が無かった。人間に確認して URP/Lit に変えた
- capture_game_view の save_path はプロジェクト基準のパスとして扱われ、Assets/ の下に保存された。消してから、一時カメラで RenderTexture に描画し、プロジェクトの外に PNG を書き出して PR に載せた
- switch-to-blender.ps1 などのパスは作業フォルダ（unity/）基準で指定する

残課題・関連Issue:
- #230: カスタマイズ画面でマテリアルの色と材質を編集する（PartMaterialSlots.GetKind で見分ける）
- これから作るモデル（#217・#225・#169 など）は Mat_Main / Mat_Sub / Mat_Light で作る
---
