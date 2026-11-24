using System;
using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>装備の組み合わせからマスタを引いて、機体の装甲値を計算する</summary>
    public static class ArmorResolver
    {
        /// <summary>装甲値を数えるスロット（フレームのパーツ）。武器・ブースター・ジェネレーターは数えない</summary>
        public static readonly PartsAttachType[] ArmorSlots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Leg,
        };

        /// <summary>
        /// フレームのパーツの装甲値を属性ごとに合計する。
        /// マスタに無いパーツ ID と未装備（負の ID）は数えない（エラーログを出さないよう TryGetValue を使う）
        /// </summary>
        /// <param name="getPartId">スロットに装備しているパーツ ID（未装備は負）</param>
        public static ArmorValues Resolve(Func<PartsAttachType, int> getPartId)
        {
            var parts = PartMaster.Instance.GetDictionary();
            var values = new List<ArmorValues>(ArmorSlots.Length);
            foreach (var slot in ArmorSlots)
            {
                var partId = getPartId(slot);
                if (partId >= 0 && parts.TryGetValue(partId, out var part) && part != null)
                    values.Add(part.Armor);
            }

            return ArmorValues.Sum(values);
        }
    }
}
