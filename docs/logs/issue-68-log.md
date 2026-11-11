---
Issue: #68 プレイヤー機体の表示モデルとパーツ（頭部・胴体・腕・脚）切り替え機能の実装
実装日: 2026-11-10 〜 2026-11-12
変更ファイル:
- blender/mech_frame.blend（新規）
- tools/blender/mech_frame_parts.py（新規）
- tools/blender/export_to_unity.py
- unity/Assets/Models/Rig/rig_mech.fbx、unity/Assets/Models/Parts/{Head,Body,Arms,Legs}/*.fbx（新規。18 個）
- unity/Assets/Animations/Mech/（MechAnimator.controller、MechAimRight.mask、MechAimLeft.mask。新規）
- unity/Assets/Prefabs/Mech/MechRig.prefab、unity/Assets/Prefabs/Parts/Frame/*.prefab（新規。12 個）
- unity/Assets/Prefabs/Parts/Resources/PartSettings/*.asset（新規。12 件）
- unity/Assets/Script/Parts/Frame/（MechRig、MechFramePart、MechBoneMap、MechFrameSlots。新規）
- unity/Assets/Script/Player/Animation/AimHoldTimer.cs（新規）
- unity/Assets/Script/Player/PlayerPartsContainer.cs、PlayerController.cs
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs、unity/Assets/Script/UI/Customize/CustomizeManager.cs
- unity/Assets/Resources/PlayerSettings/Player.prefab、PlayerNPC.prefab、PlayerPartsAssembleDataAsset.asset、NpcPartsAssembleDataAsset.asset
- unity/Assets/Tests/EditMode/MechFrameTests.cs（新規）、PartsAssembleDataTests.cs
- docs/blender-unity-pipeline.md、unity/Assets/Models/FBXImportSettings.md、.gitignore

実装方針:
- 3 つの PR に分けた（#77 共通リグと標準パーツ・アニメーション／#78 light・heavy の追加／#79 カスタマイズ画面）。#78・#79 は積み上げたブランチにマージされたため、#88 で main に取り込んだ
- 方式はスキニング＋ボーンの付け替え（人間が選択）。Blender の共通アーマチュア `rig_mech`（ボーン 20 本、機体の高さ 8 m）に全パーツをスキニングし（箱ごとに 1 本のボーンへウェイト 100%）、Unity の `MechRig` がパーツの `SkinnedMeshRenderer.bones` をボーン名で基準リグのボーンに付け替える。名前の対応付けは `MechBoneMap` に切り出してテストした
- パーツのメッシュはアーマチュアの子にせず、Armature モディファイアだけで参照する。エクスポートスクリプトが、参照するアーマチュアをパーツの FBX に含める。アニメーションは `rig_` の FBX にだけ書き出す
- Animation Type は Generic。Animator Controller は、ベースレイヤー（待機 ⇔ 歩行、`Speed` / `IsGrounded`）と、Avatar Mask で腕に限定した構えのレイヤー（`AimRight` / `AimLeft`）。射撃後 1 秒（`AimHoldTimer`）構えを保つ
- 機体の高さは途中で人間が 8 m に指定した。当たり判定（カプセルの高さ 8・半径 1.4）とカメラの追従位置も合わせ、移動パラメーターとフィールドの縮尺は変えていない（#66 に追加）
- 機体本体は外せないので、保存データにスロットが無ければアセットの初期値を使う（`PartsAssembleDataAsset.GetEquippedPartId`）。武器は従来どおり未装備にできる
- NPC 機体（PlayerNPC）にも同じリグを適用した（人間が選択）
- 各部位 3 種類（standard / light / heavy）。関節の位置は変えず、メッシュの形と太さで差をつけた

詰まった点・解決策:
- FBX の縮尺が 1/100 になった: ドキュメントとスクリプトの設定 `global_scale=0.01` が、`apply_scale_options='FBX_SCALE_ALL'` と組み合わさって 1/100 になっていた（モデルが 1 つも無かったため未検証だった）→ 1.0 に修正し、ドキュメントも直した。Unity と Blender の切り替えが 1 往復増えた
- 機体が 4 m 浮いた: アーマチュアのノードがリグのプレハブのルートになっており、Animator のクリップがルート自体の位置・回転を上書きして、プレハブで設定した位置（0, -4, 0）が 0 に戻った → 位置合わせ用の親オブジェクト `MechOffset` を挟んだ
- パーツの入れ物の名前 `Head` がボーンの `Head` と同じで紛らわしかった → `Part_Head` などに変えた
- `eval` では `using` が使えない → 型名を完全修飾する
- 歩行中につま先が地面に約 12 cm 沈む（プロトタイプとして許容）
- トークン使用量が多かった処理と改善案は `docs/modeling-work-policy.md` にまとめた（#80）

残課題・関連Issue:
- 見た目（形の違い、歩行・構えの自然さ）とカスタマイズ画面のボタン操作の目視確認
- #66: 8 m 化に合わせた移動パラメーター・フィールドの縮尺の見直し、既定の照準の高さの確認
- #70: ブースターの作り直し（取り付け位置は胸の周りに仮に移しただけ）
- NPC 機体は Play で確認できていない（ミッション 1 に NPC がいないため）
---
