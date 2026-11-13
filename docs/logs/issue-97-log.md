---
Issue: #97 エネルギー出力と回復力の計算ロジックを組み込む
実装日: 2026-11-13
変更ファイル:
- unity/Assets/Script/Master/PartsSettings/GeneratorSettings.cs（新規）、PartSettings.cs、BoosterSettings.cs
- unity/Assets/Script/Master/MasterManager.cs、PlayerParameterSettings.cs
- unity/Assets/Script/Parts/Energy/EnergyPerformance.cs、EnergyPerformanceResolver.cs（新規）
- unity/Assets/Script/Parts/PartsAssembleDataAsset.cs、Parts/Frame/MechFrameSlots.cs
- unity/Assets/Script/Player/CharacterParam.cs、PlayerController.cs、MyPlayerController.cs、PlayerPartsContainer.cs
- unity/Assets/Script/BaseCharacter.cs
- unity/Assets/Script/Npc/NpcBoostDecider.cs（新規）、NpcController.cs
- unity/Assets/Script/UI/Customize/CustomizeManager.cs、PartsSlotButton.cs
- unity/Assets/Prefabs/Parts/Resources/GeneratorSettings/Generator_Standard.asset（新規）と各パーツのマスタ、Resources/PlayerSettings/*PartsAssembleDataAsset.asset
- unity/Assets/Tests/EditMode/EnergyPerformanceTests.cs（新規）、MechFrameTests.cs、PartsAssembleDataTests.cs
- docs/spec/プレイヤー操作.md、docs/spec/基本仕様.md

実装方針:
- PR は #104 の 1 つ。回復力 ＝ ジェネレーターの総出力 − 装備した各パーツの常時消費の合計。ブースト ON の間はブースターの ON 消費（`BoosterSettings.boostOnEnergyConsumption`）も引く
- 計算は Unity に依存しない `EnergyPerformance`（最大エネルギーと、ブースト OFF / ON の回復力）。マスタを引く部分は `EnergyPerformanceResolver` に分け、スロット → パーツ ID の関数を受け取る形にして、機体（保存データ）とカスタマイズ画面（編集中の辞書）の両方から使う。マスタに無い ID は `TryGetValue` で無視する
- 着手時に決めたこと: ON の消費は地上でも加える／左右に同じ武器を持つと 2 本分を数える（スロットごとに数える）／ジェネレーターは外せないスロットとして、保存データに無ければ初期値で補う（`MechFrameSlots.IsRequiredSlot`。リグに付けないので `All` には入れない）
- ジェネレーターを読み込めないときは、`PlayerParameterSettings` の値（最大エネルギー・回復・`boostEnergy`）で `EnergyPerformance.FromFixedValues` を作る。`CharacterParam.SetInitialInfo` で先にこれを入れ、`PlayerController.Start` で装備から計算した値に置き換える
- `PlayerPartsAssembleDataAsset.Load()` を `PlayerPartsContainer.Start` から `Awake` に移した（`PlayerController.Start` が保存データを読むため。`Start` 同士の順は決まっていない）
- 浮遊中にブーストを毎秒消費していた処理は削除し、回復力の切り替えに置き換えた。`CanBoost` を削除し、基底の `CheckBoost` は `CheckAscend` と同じく false を返す。プレイヤーは入力だけ、NPC は `NpcBoostDecider`（ON で残量 10% 以下なら OFF、OFF で 50% 以上なら ON）
- カスタマイズでは、ジェネレーターが無い、または回復力が 0 以下の組み合わせのとき保存ボタンを `interactable = false` にする（`OnClickSave` でも確かめる）。理由の表示は #100
- マスタの値は、標準構成で変更前と同じ回復量（OFF 3500 / ON 2000）になるようにした。ジェネレーター 401001 は容量 10000・出力 5000。頭・胴・腕・脚の常時消費は Light 200/400/200/300、Standard 300/500/300/400、Heavy 400/600/400/500。ブースターの ON 消費は 1500、銃は 0

詰まった点・解決策:
- アセットの値は `eval` で `SerializedObject` を使って設定した。`Gun_001.asset` には、以前からある `secondaryLockTime` が既定値で書き出された（値の変化なし）
- Play 確認は、Mission 0 の Battle に `eval` で遷移し、`CharacterParam` の値を読んだ。エネルギーを 0 にしてブーストを ON にすると、約 5 秒で 9836 まで回復した（ON の回復力 2000。不足分は毎フレームの切り捨て）

残課題・関連Issue:
- 未確認: NPC のブースト切り替えの実機確認（Mission 0 に NPC がいないため。判定は EditMode テストで確認）
- 未確認: カスタマイズ画面で保存ボタンが無効になること（今のマスタでは Heavy の構成でも回復力が OFF 3100 / ON 1600 と正になり、組めない組み合わせが無い）。武器に常時消費を付けるか、出力の低いジェネレーターを追加したときに確かめる
- #98: オーバーヒート。`CharacterParam.EnergyPerformance` と `EnergyRecovery` を使う
- #100: カスタマイズ画面に性能と組めない理由を表示する。`EnergyPerformanceResolver.TryResolve` と `EnergyPerformance.IsValid` を使う
- #66 に追加: 継続消費と回復を毎フレーム `(int)` で切り捨てているため、フレームレートで実際の量が変わる／NPC のブーストの行は #104 で解決し、ジャンプの判定だけが残る
---
