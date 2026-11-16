---
Issue: #111 カスタマイズシーンにモデル確認用のデバッグモードを追加する
実装日: 2026-11-16
変更ファイル:
- unity/Assets/Script/Master/MasterManager.cs
- unity/Assets/Script/Utils/SingletonMonoObject.cs
- unity/Assets/Script/UI/Customize/CustomizeManager.cs
- unity/Assets/Script/UI/Customize/CustomizeDebugMode.cs
- unity/Assets/Script/UI/Customize/MechPreview.cs
- unity/Assets/Script/Parts/MechPartsAssembler.cs
- unity/Assets/Script/Parts/Frame/MechBody.cs
- unity/Assets/Script/Player/PlayerPartsContainer.cs
- unity/Assets/Tests/EditMode/CustomizeDebugModeTests.cs
- unity/Assets/Prefabs/Mech/MechBody.prefab
- unity/Assets/Resources/PlayerSettings/Player.prefab
- unity/Assets/Prefabs/UI/Customize/CustomizePanel.prefab
- unity/Assets/Scenes/Customize.unity

実装方針:
- MainScene を通らずに Customize シーンを Play するとデバッグモードになる（`MasterManager.IsLoaded` を追加して判定）。マスタを自分で読み込み、アセットの初期値から組み立てる。PlayerPrefs は読み書きせず、「保存」「戻る」は無効にする
- 組めない組み合わせは警告だけ出し、マスタの全パーツを選べる。将来パーツの保持情報で絞り込む位置に、デバッグモードでは使わない旨のコメントを残した
- Player.prefab の見た目の部分（`RightArm` / `LeftArm` / `Core` / `MechOffset`）を入れ子プレハブ `MechBody.prefab` に分け、組み付け処理を `MechPartsAssembler` にまとめてバトルとプレビューで共有した
- `MechPreview`: 変わったスロットだけ組み直し、RenderTexture に描いて CustomizePanel に表示する。動作（待機・歩行・空中）の切り替えと回転表示付き
- 判定と最初の組み合わせを `CustomizeDebugMode` に分けてテストを追加した
- PR #167

詰まった点・解決策:
- `SingletonMonoObject` は MainScene にしか無く、直接 Play すると `MasterManager.Instance` が null になった → `SingletonMonoObject.EnsureExists()` を追加した（#112 で `DebugBootstrap` にまとめた）
- Play 中に TMP とレイアウトが上書き値をシーンに書き込み、シーンが dirty になった → 差分を確かめ、保存せずに開き直して破棄した
- エネルギー表示の文字がはみ出した → `isTextOverflowing` で確かめながら配置を調整した

残課題・関連Issue:
- PlayerNPC.prefab も同じ構成だが `MechBody` にそろえていない
- プレビューの見た目は未確認（スクリーンショットを撮っていない）
- エフェクト確認シーンは #112、メニューからの起動は #113
---
