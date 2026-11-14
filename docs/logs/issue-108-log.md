---
Issue: #108 機体の AP が 0 になったときの撃破・任務失敗の処理
実装日: 2026-11-14
変更ファイル:
- unity/Assets/Script/HitReceiver/CharacterDeadComponent.cs（新規）、CharacterHitReceiver.cs、DeadGoalComponent.cs、HitReceiver.cs
- unity/Assets/Script/State/MissionResult.cs（新規）、StateManager.cs
- unity/Assets/Script/Player/PlayerController.cs
- unity/Assets/Script/Npc/NpcController.cs
- unity/Assets/Script/UI/Result/ResultPanel.cs（新規）
- unity/Assets/Prefabs/UI/Result/ResultPanel.prefab（新規）
- unity/Assets/Resources/PlayerSettings/Player.prefab、PlayerNPC.prefab
- unity/Assets/Scenes/Result.unity
- unity/Assets/Tests/EditMode/MissionResultHolderTests.cs（新規）
- docs/spec/撃破と任務の結果.md（新規）、docs/spec/_Sidebar.md、CLAUDE.md

実装方針:
- PR は #126 の 1 つ。仕様は Issue のコメントで決め、新しいページ「撃破と任務の結果」に書いた
- `CharacterDeadComponent`（`DeadComponent` を継承）を `Player.prefab` と `PlayerNPC.prefab` のルートに付け、`CharacterHitReceiver.deadComp` に設定した。プレイヤーと NPC は同じ処理をする
  - AP が 0 になった時点で `PlayerController.StopControl()` を呼んで操作を止める（ブースト・ハイパーブーストを切り、速度を 0 にして、コンポーネントを無効にする）。`NpcController` はオーバーライドして NavMeshAgent を止め、部隊から外す
  - 演出の後（`hideDelay` 1.5 秒）に `SetActive(false)` で非表示にする
  - 自機のときは、撃破した時点で任務の失敗を決め、非表示にしてから `resultDelay`（1 秒）後に Result へ遷移する。非表示にするとコルーチンが止まるため、遷移の待ちは UniRx の `Observable.Timer` で行う
- 任務の結果は Unity に依存しない `MissionResultHolder` に分け、`StateManager` に持たせた。Battle に入るときにリセットし、最初に決まった結果だけを残す（自機の撃破の演出中に目標を壊しても失敗のまま）。Result への遷移は `FinishBattle()` に分け、Battle 中でなければ何もしない（二重に遷移しない）。`DeadGoalComponent` は成功を決められたときだけ遷移する
- Result 画面は `ResultPanel.prefab`（TMP のテキスト、NotoSansJP）を Canvas に配置し、失敗のときだけ「Failed」と表示する。成功のときは何も出さない
- テストは `MissionResultHolderTests`（4 件）を追加した

詰まった点・解決策:
- Play での確認は、`WarGame/Play Main Scene` から始めて eval で `NextState` と `SetPendingMission` を呼び、Battle まで進めた。`NextState` を 1 回の eval で続けて呼ぶとシーンの読み込みが重なって「Now Loading Another Scene.」のエラーが出るため、1 回ずつ分けて呼ぶ
- 今のミッション（0 / 1）には NPC が出てこないため、`PlayerNPC` を手動で生成し、仮の `SquadData` に入れて確認した
- Result シーンを保存すると、Unity のバージョンアップに伴う自動の再シリアライズの差分も入った。このシーンは今回の変更で保存が必要だったため、そのまま含めた

残課題・関連Issue:
- 爆発のエフェクト（`effectId=12`）にプレハブが設定されていないため、今は 1.5 秒後に機体が消えるだけに見える。演出を付けるときに `hideDelay` を演出の長さに合わせる
- Result 画面の任務失敗の表示（文言・見た目）、成功時の表示は別途決める
- #66: `PlayerNPC.prefab` を生成すると `BoosterContainer.cs:29` で NullReferenceException が出る（今回の確認で発見し、チェックリストに追加）
- #66: NPC がゲームに出てこない件。NPC を出すようにしたら、撃破の流れ（部隊からの除外など）を実際の部隊で確かめる
- Result シーンの Canvas は `UICanvas.prefab` ではなく、ボタンもプレハブになっていない（UI 実装ポリシーと異なる。今回は触っていない）
- #115 / #116: ダメージ計算・装甲で `HitReceiver` を変更するときは、撃破の判定（`isDead`、`deadComp`）と重ならないか確かめる
---
