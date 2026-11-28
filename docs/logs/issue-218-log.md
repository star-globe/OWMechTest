---
Issue: #218 機体本体のスピードをパーツの構成（重量・ブースターなど）から決める
実装日: 2026-11-28
変更ファイル:
- unity/Assets/Script/Parts/Mobility/MobilityPerformance.cs（新規。MobilityConst、MobilityPerformance、MobilityPerformanceResolver）
- unity/Assets/Script/Master/PartsSettings/PartSettings.cs（weight、脚部用の walkSpeed・loadCapacity を追加）
- unity/Assets/Script/Master/PlayerControllerConstSettings.cs（overweightSlowRate・minWeightRate と PlayerControllerConst.Mobility を追加）
- unity/Assets/Script/Player/Movement/PlayerMoveParams.cs（WithMobility を追加）
- unity/Assets/Script/Player/PlayerController.cs（ApplyMobilityPerformance、BodySpeed を追加）
- unity/Assets/Script/Player/PlayerPartsContainer.cs（TryGetMobilityPerformance を追加）
- unity/Assets/Script/UI/Customize/CustomizeMobilityText.cs（新規）、CustomizeManager.cs（重量とスピードの表示、超過の警告）
- unity/Assets/Script/Master/PartsSettings/BoosterSettings.cs、Parts/Booster/BoostPerformance.cs（コメントの更新）
- unity/Assets/Prefabs/Parts/Resources/ の各パーツのマスタ、unity/Assets/Resources/PlayerControllerConstSettings.asset（初期値）
- unity/Assets/Tests/EditMode/MobilityPerformanceTests.cs（新規。18 件）
- docs/spec/プレイヤー操作.md（「本体のスピード」を追加）、docs/spec/基本仕様.md（重量・積載量と画面の表示）

実装方針:
- 本体のスピード = 脚部の歩行速度 × 重量補正。積載率（脚部以外の総重量 ÷ 脚部の積載量）が 1 以下なら補正は 1、超えると 1 − 超過減速率 × (積載率 − 1)（下限あり。初期値 2.0 / 0.2）
- 人間の判断で、積載量以内では遅くならず、超えたときだけ遅くする形にした。超過しても組める（保存もできる）が、カスタマイズ画面で警告する
- ブースト ON の速さ（本体のスピード × ブースト出力）、クイックブースト・ハイパーブーストの出力にも重量補正を掛ける。PlayerMoveParams.From(...).WithBoost(...).WithMobility(...) の順で作る
- 脚部が無い・マスタに無い・歩行速度が 0 のときは、今までの PlayerControllerSettings.VecSpeed を使う
- マスタの初期値は、初期の組み合わせ（総重量 4300 / 積載量 5000、歩行速度 9）が今と同じ速さになるように決めた
- NPC も PlayerController を継承しているので、同じ計算を使う

詰まった点・解決策:
- 戦闘への遷移を eval で行うと 5 秒でタイムアウトしたので、timeout を 20 秒にして FieldManager.SetPendingMission(0) と StateManager.GoToState(Battle) を呼んだ
- 銃のアセットには、今まで書かれていなかった既存の項目（装甲・バースト）が既定値のまま書き出された（値は変わらない）

残課題・関連Issue:
- 重い機体を操作して遅くなる手触りの確認と、重量・積載量の値の調整はまだ
- #216（近接武装）: 攻撃中の移動の速さに PlayerController.BodySpeed を使う
---
