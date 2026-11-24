---
Issue: #116 ダメージ計算式に攻撃力と装甲を反映する
実装日: 2026-11-25
変更ファイル:
- unity/Assets/Script/HitReceiver/DamageCalculator.cs（新規）
- unity/Assets/Script/HitReceiver/HitReceiver.cs
- unity/Assets/Script/Master/GlobalParamSettings.cs、unity/Assets/Resources/GlobalParamSettings.asset
- unity/Assets/Tests/EditMode/DamageCalculatorTests.cs（新規）
- unity/Assets/Prefabs/Parts/Resources/PartSettings/（フレーム 12 パーツの装甲値）
- unity/Assets/Resources/UnitSettings/（戦車・砲台・ドローンの装甲値）
- unity/Assets/Prefabs/Bullets/Resources/BulletSettings/Bullet_001.asset（攻撃力 1 → 100）
- docs/spec/武装.md（「ダメージ計算」の節を追加）、docs/spec/ユニット全般.md

実装方針:
- 割合型の式にした: ダメージ = 攻撃力 × 基準値 ÷ (基準値 + 攻撃と同じ属性の装甲値)。四捨五入し、攻撃力が 1 以上なら最低 1。装甲を持たない対象（`Armor == null`）は攻撃力そのまま
- 減算型（攻撃力 - 装甲）は、攻撃力と装甲の値の大きさを常にそろえないと最低ダメージに張り付くため選ばなかった
- 基準値は `GlobalParamSettings.armorReferenceValue`（初期値 1000）。装甲値が基準値と同じときダメージが半分になる
- 計算は Unity に依存しない static クラス `DamageCalculator` にまとめ、`HitReceiver.Damage` から呼ぶ
- 装甲の初期値（実弾 / EN の合計）: Light 240 / 300、Standard 450 / 450、Heavy 670 / 540（胴 40%・脚 27%・腕 20%・頭 13% で割り振り）。戦車 300 / 150、砲台 400 / 200、ドローン 50 / 50

詰まった点・解決策:
- 今の攻撃力（ライフル 1、ミサイル 5）では最低 1 に張り付いて装甲が効かないため、ライフルの弾を 100 にした
- ミサイルの弾（Missile_001）は #183 が隣の行（エフェクトの ID）を変えていたため、衝突を避けて 5 のまま残した
- アセットは SerializedObject で設定したため、今まで書き出されていなかった既存の項目が既定値で加わった（値は変わらない）

残課題・関連Issue:
- Missile_001 の攻撃力を 500 程度に見直す（#66 に追加済み。#183 はマージ済みなので着手できる）。それまではミサイルがライフルより弱い
- #117（EN 射撃武器）の EN 弾の攻撃力は、この式の規模（ライフル 100 前後）に合わせる
- 戦闘で弾が当たってダメージが入るところは、Play では確かめていない（Main Scene の起動までを確認した）
---
