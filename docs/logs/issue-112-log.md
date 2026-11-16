---
Issue: #112 パーツのエフェクト確認用シーンを作る
実装日: 2026-11-16
変更ファイル:
- unity/Assets/Script/EffectCheck/PartEffectTester.cs
- unity/Assets/Script/EffectCheck/BoosterEffectTester.cs
- unity/Assets/Script/EffectCheck/BoosterEffectCheckState.cs
- unity/Assets/Script/EffectCheck/EffectCheckCamera.cs
- unity/Assets/Script/Utils/DebugBootstrap.cs
- unity/Assets/Script/UI/Customize/CustomizeManager.cs
- unity/Assets/Tests/EditMode/BoosterEffectCheckStateTests.cs
- unity/Assets/Scenes/Debug/EffectCheck.unity
- unity/Assets/Prefabs/UI/EffectCheck/EffectCheckPanel.prefab
- CLAUDE.md
- unity/.claude/settings.json
- unity/.claude/skills/unity-verify/SKILL.md

実装方針:
- `Scenes/Debug/EffectCheck.unity`（ビルド対象外）で、ブースターをマスタから選んで台に 1 つ呼び出し、ゲーム内と同じ呼び方（ブーストとハイパーは毎フレーム、クイックは押した瞬間）で噴射させる。基本色はパーツの色とプリセット 4 色で切り替える
- 「出力の強弱」は、人間と相談して当面は通常・クイック・ハイパーの切り替えで代えた（`BoosterPart` に出力の API は足していない）
- `PartEffectTester`（一覧・生成・差し替え）を基底にし、武器は派生クラスを足せば対応できる構成にした
- MainScene を通らないときの初期化を `DebugBootstrap` にまとめ、#111 のデバッグモードもこれを使うようにした
- 人間の依頼で、直接 Play してよいシーンを CLAUDE.md に書き、`open_scene` / `editor_play` を許可リストに足した（`editor_play` は #113 で外した）
- PR #170

詰まった点・解決策:
- 直接 Play の承認がまだ無いと親セッションから訂正があり、Play の前で止まって承認を待った
- UI は CustomizePanel の一覧・ボタン・テキストを複製して作り、`PartsSelectButton` と `ObjectPool` をそのまま使った

残課題・関連Issue:
- 炎の見た目は未確認（スクリーンショットを撮っていない）
- 武器（発射・着弾）の確認は、射撃系のエフェクトを付けるときに `GunEffectTester` を足す
- #70（ブースターのエフェクトの品質向上）の確認に使う
---
