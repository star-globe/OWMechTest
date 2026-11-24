using System.Collections.Generic;
using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class PartSlotRuleTests
    {
        [Test]
        public void 腕にはライフルだけサブにはミサイルだけを付けられる()
        {
            Assert.IsTrue(PartSlotRule.CanEquip(PartsAttachType.Weapon_Right, PartCategoryType.Gun));
            Assert.IsTrue(PartSlotRule.CanEquip(PartsAttachType.Weapon_Left, PartCategoryType.Gun));
            Assert.IsFalse(PartSlotRule.CanEquip(PartsAttachType.Weapon_Right, PartCategoryType.Missile));
            Assert.IsFalse(PartSlotRule.CanEquip(PartsAttachType.Weapon_Left, PartCategoryType.Missile));

            Assert.IsTrue(PartSlotRule.CanEquip(PartsAttachType.Weapon_Sub, PartCategoryType.Missile));
            Assert.IsFalse(PartSlotRule.CanEquip(PartsAttachType.Weapon_Sub, PartCategoryType.Gun));
        }

        [Test]
        public void 機体本体のスロットには同じカテゴリだけを付けられる()
        {
            Assert.IsTrue(PartSlotRule.CanEquip(PartsAttachType.Head, PartCategoryType.Head));
            Assert.IsFalse(PartSlotRule.CanEquip(PartsAttachType.Head, PartCategoryType.Core));
            Assert.IsTrue(PartSlotRule.CanEquip(PartsAttachType.Generator, PartCategoryType.Generator));
            Assert.IsFalse(PartSlotRule.CanEquip(PartsAttachType.None, PartCategoryType.None));
        }

        [Test]
        public void 付けられないパーツが入っているスロットを返す()
        {
            var parts = new Dictionary<PartsAttachType, int>
            {
                { PartsAttachType.Weapon_Right, 1 },   // ライフル
                { PartsAttachType.Weapon_Left, 2 },    // ミサイル（不可）
                { PartsAttachType.Weapon_Sub, 1 },     // ライフル（不可）
            };
            var categories = new Dictionary<int, PartCategoryType>
            {
                { 1, PartCategoryType.Gun },
                { 2, PartCategoryType.Missile },
            };

            var invalid = PartSlotRule.FindInvalidSlots(PartSlotRule.WeaponSlots,
                slot => parts.TryGetValue(slot, out var id) ? id : -1,
                id => categories.TryGetValue(id, out var c) ? c : (PartCategoryType?)null);

            CollectionAssert.AreEquivalent(new[] { PartsAttachType.Weapon_Left, PartsAttachType.Weapon_Sub }, invalid);
        }

        [Test]
        public void 未装備とマスタに無いパーツは数えない()
        {
            var invalid = PartSlotRule.FindInvalidSlots(PartSlotRule.WeaponSlots,
                slot => slot == PartsAttachType.Weapon_Sub ? 99 : -1,
                id => null);

            Assert.IsEmpty(invalid);
        }
    }
}
