using System;
using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>装備の組み合わせからマスタを引いて、エネルギーの性能を計算する</summary>
    public static class EnergyPerformanceResolver
    {
        /// <summary>常時消費を数えるスロット。左右の武器はそれぞれ数える</summary>
        public static readonly PartsAttachType[] ConsumingSlots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Leg,
            PartsAttachType.Generator,
            PartsAttachType.Booster,
            PartsAttachType.Weapon_Right,
            PartsAttachType.Weapon_Left,
            PartsAttachType.Weapon_Sub,
        };

        /// <summary>
        /// 性能を計算する。ジェネレーターがマスタに無ければ false。
        /// マスタに無いパーツ ID はエラーログを出さずに無視する（GetSettings ではなく TryGetValue を使う）
        /// </summary>
        /// <param name="getPartId">スロットに装備しているパーツ ID（未装備は負）</param>
        public static bool TryResolve(Func<PartsAttachType, int> getPartId, out EnergyPerformance performance)
        {
            performance = default;

            var generatorId = getPartId(PartsAttachType.Generator);
            if (!GeneratorMaster.Instance.GetDictionary().TryGetValue(generatorId, out var generator) || generator == null)
                return false;

            var parts = PartMaster.Instance.GetDictionary();
            var consumptions = new List<int>(ConsumingSlots.Length);
            foreach (var slot in ConsumingSlots)
            {
                var partId = getPartId(slot);
                if (partId >= 0 && parts.TryGetValue(partId, out var part) && part != null)
                    consumptions.Add(part.ConstantEnergyConsumption);
            }

            var boostOn = 0;
            var boosterId = getPartId(PartsAttachType.Booster);
            if (boosterId >= 0 && BoosterMaster.Instance.GetDictionary().TryGetValue(boosterId, out var booster) && booster != null)
                boostOn = booster.BoostOnEnergyConsumption;

            performance = EnergyPerformance.Calc(generator.EnergyCapacity, generator.EnergyOutput, consumptions, boostOn);
            return true;
        }
    }
}
