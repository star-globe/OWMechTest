using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class MissileGuidanceTests
    {
        [Test]
        public void 旋回性能か誘導時間が0なら誘導しない()
        {
            Assert.IsTrue(MissileGuidance.IsHoming(90f, 2f));
            Assert.IsFalse(MissileGuidance.IsHoming(0f, 2f));
            Assert.IsFalse(MissileGuidance.IsHoming(90f, 0f));
        }

        [Test]
        public void 誘導が始まるまでは直進し誘導時間が過ぎたら直進に戻る()
        {
            // 境界の値は 2 進数で正確に表せる値にする（浮動小数の誤差で段階が変わらないように）
            Assert.AreEqual(GuidancePhase.BeforeGuidance, MissileGuidance.GetPhase(0.1f, 0.25f, 2f, true));
            Assert.AreEqual(GuidancePhase.Guiding, MissileGuidance.GetPhase(0.25f, 0.25f, 2f, true));
            Assert.AreEqual(GuidancePhase.Guiding, MissileGuidance.GetPhase(2.2f, 0.25f, 2f, true));
            Assert.AreEqual(GuidancePhase.Straight, MissileGuidance.GetPhase(2.25f, 0.25f, 2f, true));
        }

        [Test]
        public void 目標が無ければ誘導しない()
        {
            Assert.AreEqual(GuidancePhase.Straight, MissileGuidance.GetPhase(0f, 0.3f, 2f, false));
            Assert.AreEqual(GuidancePhase.Straight, MissileGuidance.GetPhase(1f, 0.3f, 2f, false));
        }

        [Test]
        public void 撃破された目標や破棄された目標は失ったとみなす()
        {
            Assert.IsFalse(MissileGuidance.IsTargetLost(true, true, 100, 100));
            Assert.IsTrue(MissileGuidance.IsTargetLost(true, true, 0, 100));
            Assert.IsTrue(MissileGuidance.IsTargetLost(false, false, 100, 100));
            Assert.IsTrue(MissileGuidance.IsTargetLost(true, false, 100, 100));
            // AP を持たない目標は AP で判定しない
            Assert.IsFalse(MissileGuidance.IsTargetLost(true, true, int.MinValue, int.MinValue));
        }

        [Test]
        public void 一回に曲がれる角度は旋回性能で制限される()
        {
            // 90 度/秒で 0.5 秒 → 45 度だけ曲がる
            var dir = MissileGuidance.TurnTowards(Vector3.forward, Vector3.right, 90f, 0.5f);
            Assert.AreEqual(45f, Vector3.Angle(Vector3.forward, dir), 0.01f);
            Assert.AreEqual(1f, dir.magnitude, 1e-4f);
        }

        [Test]
        public void 上限より小さい角度なら目指す向きにそろう()
        {
            var desired = Quaternion.Euler(0f, 10f, 0f) * Vector3.forward;
            var dir = MissileGuidance.TurnTowards(Vector3.forward * 50f, desired * 3f, 90f, 0.5f);
            Assert.AreEqual(0f, Vector3.Angle(desired, dir), 0.01f);
        }

        [Test]
        public void 一次ロックは現在位置二次ロックは未来位置へ向かう()
        {
            var missile = Vector3.zero;
            var target = new Vector3(0f, 0f, 100f);
            var targetVel = new Vector3(10f, 0f, 0f);

            Assert.AreEqual(target, MissileGuidance.CalcAimPoint(LockLevel.Primary, missile, 100f, target, targetVel));

            var lead = MissileGuidance.CalcAimPoint(LockLevel.Secondary, missile, 100f, target, targetVel);
            Assert.Greater(lead.x, 0f);
            // 未来位置は、ミサイルが届くまでの時間だけ目標が進んだ位置
            Assert.AreEqual(lead.magnitude / 100f, (lead - target).magnitude / 10f, 1e-3f);
        }
    }
}
