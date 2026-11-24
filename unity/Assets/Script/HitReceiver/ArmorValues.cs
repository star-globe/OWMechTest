using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>
    /// 攻撃の属性（AttackAttribute）ごとの装甲値。
    /// 装甲を持たない対象は、この値ではなく null（HitReceiver.Armor）で表す（装甲値 0 とは区別する）
    /// </summary>
    public readonly struct ArmorValues
    {
        /// <summary>実弾装甲</summary>
        public readonly int Physical;
        /// <summary>EN 装甲</summary>
        public readonly int Energy;

        public ArmorValues(int physical, int energy)
        {
            Physical = physical;
            Energy = energy;
        }

        public static ArmorValues Zero => new ArmorValues(0, 0);

        /// <summary>攻撃の属性に対応する装甲値</summary>
        public int Get(AttackAttribute attribute)
        {
            return attribute switch
            {
                AttackAttribute.Physical => Physical,
                AttackAttribute.Energy => Energy,
                _ => 0,
            };
        }

        public static ArmorValues operator +(ArmorValues a, ArmorValues b)
            => new ArmorValues(a.Physical + b.Physical, a.Energy + b.Energy);

        /// <summary>属性ごとに合計する（機体の装甲値はフレームのパーツの合計）</summary>
        public static ArmorValues Sum(IEnumerable<ArmorValues> values)
        {
            var total = Zero;
            if (values == null)
                return total;

            foreach (var value in values)
                total += value;

            return total;
        }

        public override string ToString() => $"Physical:{Physical} Energy:{Energy}";
    }
}
