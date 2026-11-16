using System.Collections.Generic;
using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class CustomizeDebugModeTests
    {
        static readonly PartsAttachType[] Slots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Generator,
            PartsAttachType.Weapon_Right,
        };

        // 装備中（保存データを反映）は 100 番台、初期値は 200 番台。武器の初期値は未装備（-1）
        static int Equipped(PartsAttachType slot) => slot switch
        {
            PartsAttachType.Head      => 101,
            PartsAttachType.Generator => 102,
            _                         => 103,
        };

        static int Default(PartsAttachType slot) => slot switch
        {
            PartsAttachType.Head      => 201,
            PartsAttachType.Generator => 202,
            _                         => -1,
        };

        // ── デバッグモードの判定 ──────────────────────────

        [Test]
        public void IsDebugMode_MasterNotLoaded_True()
        {
            Assert.IsTrue(CustomizeDebugMode.IsDebugMode(isMasterLoaded: false));
        }

        [Test]
        public void IsDebugMode_MasterLoaded_False()
        {
            Assert.IsFalse(CustomizeDebugMode.IsDebugMode(isMasterLoaded: true));
        }

        // ── 保存・戻る ──────────────────────────

        [Test]
        public void CanSave_Normal_FollowsAssemblable()
        {
            Assert.IsTrue(CustomizeDebugMode.CanSave(isDebugMode: false, isAssemblable: true));
            Assert.IsFalse(CustomizeDebugMode.CanSave(isDebugMode: false, isAssemblable: false));
        }

        [Test]
        public void CanSave_Debug_AlwaysFalse()
        {
            Assert.IsFalse(CustomizeDebugMode.CanSave(isDebugMode: true, isAssemblable: true));
            Assert.IsFalse(CustomizeDebugMode.CanSave(isDebugMode: true, isAssemblable: false));
        }

        [Test]
        public void CanGoBack_OnlyNormal()
        {
            Assert.IsTrue(CustomizeDebugMode.CanGoBack(isDebugMode: false));
            Assert.IsFalse(CustomizeDebugMode.CanGoBack(isDebugMode: true));
        }

        // ── 最初の組み合わせ ──────────────────────────

        [Test]
        public void BuildInitialAssembly_Normal_UsesEquipped()
        {
            var dic = CustomizeDebugMode.BuildInitialAssembly(Slots, false, Equipped, Default);

            Assert.AreEqual(3, dic.Count);
            Assert.AreEqual(101, dic[PartsAttachType.Head]);
            Assert.AreEqual(102, dic[PartsAttachType.Generator]);
            Assert.AreEqual(103, dic[PartsAttachType.Weapon_Right]);
        }

        [Test]
        public void BuildInitialAssembly_Debug_UsesDefaultAndIgnoresSaved()
        {
            var calledEquipped = false;
            var dic = CustomizeDebugMode.BuildInitialAssembly(Slots, true,
                slot => { calledEquipped = true; return Equipped(slot); }, Default);

            Assert.IsFalse(calledEquipped, "デバッグモードでは保存データを反映した値を見ない");
            Assert.AreEqual(201, dic[PartsAttachType.Head]);
            Assert.AreEqual(202, dic[PartsAttachType.Generator]);
            Assert.AreEqual(-1, dic[PartsAttachType.Weapon_Right]);
        }

        // ── プレビューの組み直し ──────────────────────────

        [Test]
        public void GetChangedSlots_NoPrevious_AllSlots()
        {
            var current = new Dictionary<PartsAttachType, int>
            {
                { PartsAttachType.Head, 1 },
                { PartsAttachType.Booster, 2 },
            };

            CollectionAssert.AreEquivalent(
                new[] { PartsAttachType.Head, PartsAttachType.Booster },
                MechPreview.GetChangedSlots(new Dictionary<PartsAttachType, int>(), current));
        }

        [Test]
        public void GetChangedSlots_OnlyChangedSlots()
        {
            var previous = new Dictionary<PartsAttachType, int>
            {
                { PartsAttachType.Head, 1 },
                { PartsAttachType.Booster, 2 },
            };
            var current = new Dictionary<PartsAttachType, int>
            {
                { PartsAttachType.Head, 1 },
                { PartsAttachType.Booster, 3 },
                { PartsAttachType.Weapon_Left, -1 },
            };

            CollectionAssert.AreEquivalent(
                new[] { PartsAttachType.Booster, PartsAttachType.Weapon_Left },
                MechPreview.GetChangedSlots(previous, current));
        }

        [Test]
        public void NextMotion_Cycles()
        {
            Assert.AreEqual(MechPreview.MotionType.Walk, MechPreview.NextMotion(MechPreview.MotionType.Idle));
            Assert.AreEqual(MechPreview.MotionType.Air, MechPreview.NextMotion(MechPreview.MotionType.Walk));
            Assert.AreEqual(MechPreview.MotionType.Idle, MechPreview.NextMotion(MechPreview.MotionType.Air));
        }
    }
}
