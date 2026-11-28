---
Issue: #221 ブースト・クイックブースト・ハイパーブーストの性能をブースターパーツで決める
実装日: 2026-11-28
変更ファイル:
- unity/Assets/Script/Master/PartsSettings/BoosterSettings.cs（6 つの値を追加。未使用の boostPower を倍率 boostSpeedRate に置き換え）
- unity/Assets/Script/Parts/Booster/BoostPerformance.cs（新規。BoostPerformance と BoostPerformanceResolver）
- unity/Assets/Script/Player/Movement/PlayerMoveParams.cs（WithBoost と、BoostPerformance を受け取る From）
- unity/Assets/Script/Player/PlayerController.cs（Start でブーストの性能を決めて保持する）
- unity/Assets/Script/Player/CharacterParam.cs（SetBoostPerformance）
- unity/Assets/Script/Player/PlayerPartsContainer.cs（GetBoostPerformance）
- unity/Assets/Script/Master/PlayerControllerSettings.cs, PlayerParameterSettings.cs（「ブースターを読み込めないときの値」の Tooltip）
- unity/Assets/Prefabs/Parts/Resources/BoosterSettings/Booster_001.asset, Booster_002.asset
- unity/Assets/Tests/EditMode/BoostPerformanceTests.cs（新規。7 件）
- docs/spec/プレイヤー操作.md, docs/spec/基本仕様.md

実装方針:
- 6 つの値（ブースト出力の倍率、クイックブーストの出力・噴射時間・消費、ハイパーブーストの出力・消費）を BoosterSettings に持たせ、EnergyPerformanceResolver と同じ形の Resolver で装備から引く
- 未装備・マスタに無い ID のときは共通設定（PlayerControllerSettings / PlayerParameterSettings）の値を使う。共通設定の項目は削除せず、その値として残した（ジェネレーターと同じ扱い）
- ブースト出力は今の倍率のまま（暫定。本体のスピードとの関係は #218 で決める）
- NPC は PlayerController を継承しているので、同じくブースターで決まる
- Booster_001 は改修前の実際の値（倍率 2、クイック 15 / 0.1 秒 / 2500、ハイパー 30 / 4000）、Booster_002 は高出力・高消費（2.2、17 / 0.1 秒 / 3000、33 / 4800）
- 移動のパラメーターの差し替えは PlayerMoveParams.WithBoost に分け、PlayerControllerConst（Resources の読み込み）に依存せずテストできるようにした

詰まった点・解決策:
- Issue に書かれた初期値（倍率 1.5 など）はコードの既定値で、実際のアセットの値（倍率 2 など）と違っていた。動きを変えないよう、アセットの値を Booster_001 に入れた
- set_serialized_field で値を変えてもファイルに書き出されず、変更していない Booster_001 は dirty にもならなかった。eval で EditorUtility.SetDirty と AssetDatabase.SaveAssetIfDirty を呼んで保存した（再シリアライズで既存の装甲の項目 0 も書き出された）
- 戦闘での確認は、eval で FieldManager.SetPendingMission(0) と StateManager.GoToState(Battle) を呼んで進めた

残課題・関連Issue:
- #66 に追加: NpcControllerSettings がどこからも使われていない／カスタマイズ画面にブースト性能が表示されない
- Booster_002 に替えたときの操作感と、NPC の機体の値は Play で確かめていない
- 後続: #218（本体のスピードをパーツの構成から決める）
---
