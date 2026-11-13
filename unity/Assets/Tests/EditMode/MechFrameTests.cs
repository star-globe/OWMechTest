using System.Collections.Generic;
using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class MechFrameTests
    {
        static List<KeyValuePair<string, string>> Pairs(params string[] names)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (var n in names)
                list.Add(new KeyValuePair<string, string>(n, "rig:" + n));
            return list;
        }

        // ── MechBoneMap ────────────────────────────────

        [Test]
        public void BuildDictionary_KeepsFirstAndReportsDuplicates()
        {
            var bones = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Hips", "first"),
                new KeyValuePair<string, string>("Spine", "spine"),
                new KeyValuePair<string, string>("Hips", "second"),
            };
            var duplicates = new List<string>();

            var dic = MechBoneMap.BuildDictionary(bones, duplicates);

            Assert.AreEqual(2, dic.Count);
            Assert.AreEqual("first", dic["Hips"]);
            CollectionAssert.AreEqual(new[] { "Hips" }, duplicates);
        }

        [Test]
        public void Remap_FollowsSourceOrder()
        {
            var target = MechBoneMap.BuildDictionary(Pairs("Root", "Hips", "Spine", "Hand_L"));

            var result = MechBoneMap.Remap(new[] { "Hand_L", "Root", "Spine" }, target);

            CollectionAssert.AreEqual(new[] { "rig:Hand_L", "rig:Root", "rig:Spine" }, result);
        }

        [Test]
        public void Remap_MissingBoneIsNullAndReported()
        {
            var target = MechBoneMap.BuildDictionary(Pairs("Root", "Hips"));
            var missing = new List<string>();

            var result = MechBoneMap.Remap(new[] { "Hips", "Tail", null }, target, missing);

            Assert.AreEqual("rig:Hips", result[0]);
            Assert.IsNull(result[1]);
            Assert.IsNull(result[2]);
            CollectionAssert.AreEqual(new[] { "Tail", null }, missing);
        }

        // ── MechFrameSlots ─────────────────────────────

        [Test]
        public void IsFrameSlot_OnlyHeadCoreArmLeg()
        {
            foreach (var slot in MechFrameSlots.All)
                Assert.IsTrue(MechFrameSlots.IsFrameSlot(slot), slot.ToString());

            Assert.IsFalse(MechFrameSlots.IsFrameSlot(PartsAttachType.Booster));
            Assert.IsFalse(MechFrameSlots.IsFrameSlot(PartsAttachType.Weapon_Right));
            Assert.IsFalse(MechFrameSlots.IsFrameSlot(PartsAttachType.None));
        }

        [Test]
        public void IsRequiredSlot_FrameAndGenerator()
        {
            foreach (var slot in MechFrameSlots.All)
                Assert.IsTrue(MechFrameSlots.IsRequiredSlot(slot), slot.ToString());

            Assert.IsTrue(MechFrameSlots.IsRequiredSlot(PartsAttachType.Generator));
            // ジェネレーターはリグに取り付けないので、本体のスロットには含めない
            Assert.IsFalse(MechFrameSlots.IsFrameSlot(PartsAttachType.Generator));
            Assert.IsFalse(MechFrameSlots.IsRequiredSlot(PartsAttachType.Booster));
            Assert.IsFalse(MechFrameSlots.IsRequiredSlot(PartsAttachType.Weapon_Sub));
        }

        [Test]
        public void ResolvePartId_UsesDefaultOnlyWhenUnset()
        {
            Assert.AreEqual(101001, MechFrameSlots.ResolvePartId(101001, 999));
            Assert.AreEqual(999, MechFrameSlots.ResolvePartId(-1, 999));
            Assert.AreEqual(0, MechFrameSlots.ResolvePartId(0, 999));
        }

        // ── AimHoldTimer ───────────────────────────────

        [Test]
        public void AimHoldTimer_ActiveUntilHoldTimeElapses()
        {
            var timer = new AimHoldTimer(1.0f);
            Assert.IsFalse(timer.IsActive);

            timer.Trigger();
            timer.Tick(0.6f);
            Assert.IsTrue(timer.IsActive);

            timer.Tick(0.6f);
            Assert.IsFalse(timer.IsActive);
        }

        [Test]
        public void AimHoldTimer_TriggerRestartsHold()
        {
            var timer = new AimHoldTimer(1.0f);
            timer.Trigger();
            timer.Tick(0.8f);

            timer.Trigger();
            timer.Tick(0.8f);
            Assert.IsTrue(timer.IsActive);
        }

        [Test]
        public void AimHoldTimer_ResetStopsImmediately()
        {
            var timer = new AimHoldTimer(1.0f);
            timer.Trigger();

            timer.Reset();
            Assert.IsFalse(timer.IsActive);
        }
    }
}
