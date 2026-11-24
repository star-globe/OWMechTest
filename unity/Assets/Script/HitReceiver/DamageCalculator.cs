using System;

namespace AdvancedGears
{
    /// <summary>
    /// 攻撃と被弾側の装甲からダメージを計算する（仕様: docs/spec/武装.md「ダメージ計算」）。
    /// Unity に依存しない純粋な計算にして、EditMode テストで確かめる
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// AP から引くダメージ。
        /// 装甲あり: 攻撃力 × 基準値 ÷ (基準値 + 攻撃と同じ属性の装甲値) を四捨五入。攻撃力が 1 以上なら最低 1
        /// 装甲なし（armor が null）: 攻撃力そのまま
        /// </summary>
        /// <param name="attack">攻撃力と属性</param>
        /// <param name="armor">被弾側の属性ごとの装甲値。装甲を持たない対象は null</param>
        /// <param name="referenceValue">装甲の基準値。装甲値がこの値と同じとき、ダメージは半分になる</param>
        public static int Calculate(AttackInfo attack, ArmorValues? armor, int referenceValue)
        {
            if (attack.Power <= 0)
                return 0;

            // 装甲を持たない対象は、攻撃力がそのままダメージになる
            if (!armor.HasValue)
                return attack.Power;

            // 基準値が不正なときは、装甲が効かないものとして扱う（マスタの設定ミスで 0 除算にしない）
            if (referenceValue <= 0)
                return attack.Power;

            var armorValue = Math.Max(0, armor.Value.Get(attack.Attribute));
            var rate = (double) referenceValue / (referenceValue + armorValue);
            var damage = (int) Math.Round(attack.Power * rate, MidpointRounding.AwayFromZero);

            return Math.Max(1, damage);
        }
    }
}
