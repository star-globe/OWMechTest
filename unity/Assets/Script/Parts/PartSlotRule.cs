using System;
using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>
    /// スロットに付けられるパーツのカテゴリ（Unity に依存しないのでテストできる）。
    /// 腕は手に持つ武器（Gun）、サブ（肩）はサブ装備のカテゴリだけを付けられる。
    /// サブ装備のカテゴリを増やすときは SubCategories に足す
    /// </summary>
    public static class PartSlotRule
    {
        static readonly PartCategoryType[] ArmWeaponCategories = { PartCategoryType.Gun };
        static readonly PartCategoryType[] SubCategories = { PartCategoryType.Missile };

        static readonly PartCategoryType[] Empty = Array.Empty<PartCategoryType>();

        /// <summary>スロットに付けられるカテゴリ。付けられるものが無いスロットは空</summary>
        public static IReadOnlyList<PartCategoryType> GetAllowedCategories(PartsAttachType slot)
        {
            switch (slot)
            {
                case PartsAttachType.Head:         return new[] { PartCategoryType.Head };
                case PartsAttachType.Core:         return new[] { PartCategoryType.Core };
                case PartsAttachType.Arm:          return new[] { PartCategoryType.Arm };
                case PartsAttachType.Leg:          return new[] { PartCategoryType.Leg };
                case PartsAttachType.Generator:    return new[] { PartCategoryType.Generator };
                case PartsAttachType.Booster:      return new[] { PartCategoryType.Booster };
                case PartsAttachType.Weapon_Right:
                case PartsAttachType.Weapon_Left:  return ArmWeaponCategories;
                case PartsAttachType.Weapon_Sub:   return SubCategories;
                default:                           return Empty;
            }
        }

        /// <summary>そのカテゴリのパーツをスロットに付けられるか</summary>
        public static bool CanEquip(PartsAttachType slot, PartCategoryType category)
        {
            var allowed = GetAllowedCategories(slot);
            for (int i = 0; i < allowed.Count; i++)
            {
                if (allowed[i] == category)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 付けられないカテゴリのパーツが入っているスロットを返す（すべて付けられるなら空）。
        /// 未装備（負の ID）と、マスタに無いパーツ（getCategory が null を返す）は数えない
        /// </summary>
        /// <param name="slots">確かめるスロット</param>
        /// <param name="getPartId">スロットのパーツ ID（未装備は負）</param>
        /// <param name="getCategory">パーツ ID のカテゴリ。マスタに無ければ null</param>
        public static List<PartsAttachType> FindInvalidSlots(IEnumerable<PartsAttachType> slots,
                                                             Func<PartsAttachType, int> getPartId,
                                                             Func<int, PartCategoryType?> getCategory)
        {
            var invalid = new List<PartsAttachType>();
            foreach (var slot in slots)
            {
                var partId = getPartId(slot);
                if (partId < 0)
                    continue;

                var category = getCategory(partId);
                if (category.HasValue && !CanEquip(slot, category.Value))
                    invalid.Add(slot);
            }

            return invalid;
        }

        /// <summary>カテゴリを確かめるスロット（武器とサブ装備）</summary>
        public static readonly PartsAttachType[] WeaponSlots =
        {
            PartsAttachType.Weapon_Right,
            PartsAttachType.Weapon_Left,
            PartsAttachType.Weapon_Sub,
        };

        /// <summary>マスタのカテゴリを引く（FindInvalidSlots に渡す）。エラーログを出さないよう TryGetValue を使う</summary>
        public static PartCategoryType? GetMasterCategory(int partId)
        {
            return PartMaster.Instance.GetDictionary().TryGetValue(partId, out var part) && part != null
                ? part.CategoryType
                : (PartCategoryType?)null;
        }
    }
}
