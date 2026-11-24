---
Issue: #115 装甲に属性（実弾 / EN）を追加する（プレイヤー・NPC）
実装日: 2026-11-24
変更ファイル:
- unity/Assets/Script/HitReceiver/ArmorValues.cs（新規）
- unity/Assets/Script/Parts/Armor/ArmorResolver.cs（新規）
- unity/Assets/Script/HitReceiver/HitReceiver.cs、CharacterHitReceiver.cs、UnitHitReceiver.cs
- unity/Assets/Script/Master/PartsSettings/PartSettings.cs
- unity/Assets/Script/Master/UnitParameterSettings/UnitParameterSettings.cs
- unity/Assets/Script/Player/CharacterParam.cs、PlayerController.cs、PlayerPartsContainer.cs
- unity/Assets/Script/Unit/UnitParam.cs
- unity/Assets/Tests/EditMode/ArmorValuesTests.cs（新規）
- docs/spec/ユニット全般.md（「装甲」の節を追加）

実装方針:
- 属性ごとの装甲値 `ArmorValues`（実弾・EN）を追加し、`Get(AttackAttribute)` で攻撃と同じ属性の装甲値を取れるようにした
- 被弾側（`HitReceiver`）に `ArmorValues? Armor` を追加した。null は「装甲を持たない」で、装甲値 0 と区別する（#116 で扱いを分けるため）
- 機体（プレイヤー・NPC）の装甲値は、フレームのパーツ（頭・胴・腕・脚）の装甲値の合計。部位ごとには分けない。エネルギーの計算（`EnergyPerformanceResolver`）と同じ形の `ArmorResolver` で計算する
- ユニットは `UnitParameterSettings` に装甲値と「装甲を持つか」を持たせた。目標オブジェクト（`FixedHitReceiver`）は装甲なし
- 装甲値はすべて 0 のまま（値の大きさは計算式で意味が変わるため、#116 で式と一緒に決める）

詰まった点・解決策:
- 装甲値の項目を `PartSettings` に置くと、武器などフレーム以外のパーツも項目を持つ。フレーム専用のクラスを作ると既存の 12 個のアセットのスクリプト参照が変わるため、`PartSettings` に置いて、数えるスロットを `ArmorResolver` で絞った
- アセットとプレハブを変更せずに済ませたので、#183（ミサイルのモデル・エフェクト）とは衝突しなかった
- 「装甲を持たない」ユニットのテストは、`SerializedObject` で `hasArmor` を書き換えて確かめた

残課題・関連Issue:
- #116（ダメージ計算式）: `HitReceiver.Damage` の中で `Armor` と攻撃の情報を計算クラスに渡す。フレームとユニットの装甲値もここで決める
- 戦闘中に機体の装甲値が設定される処理（`PlayerController.Start`）は、Play では通していない（Main Scene の起動までを確認した）
---
