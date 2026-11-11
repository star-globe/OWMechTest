using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class LeadCalculatorTests
    {
        const float Tolerance = 1e-3f;

        static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, "z");
        }

        /// <summary>照準点へ撃った弾（初速 = 照準方向 × 弾速 + 撃った側の速度）が、等速で動く目標に当たるかを確かめる</summary>
        static void AssertHits(Vector3 muzzle, Vector3 shooterVel, Vector3 targetPos, Vector3 targetVel, float speed, Vector3 aimPoint)
        {
            var bulletVel = (aimPoint - muzzle).normalized * speed + shooterVel;
            var d = targetPos - muzzle;
            var v = targetVel - shooterVel;
            Assert.IsTrue(LeadCalculator.TryCalcHitTime(d, v, speed, out var t));

            var bulletPos = muzzle + bulletVel * t;
            var targetAt = targetPos + targetVel * t;
            AssertVector(targetAt, bulletPos);
        }

        [Test]
        public void 静止した目標と静止した自機では目標の位置をそのまま狙う()
        {
            var muzzle = Vector3.zero;
            var target = new Vector3(0, 0, 100);

            Assert.IsTrue(LeadCalculator.TryCalcAimPoint(muzzle, Vector3.zero, target, Vector3.zero, 50f, out var aim));
            AssertVector(target, aim);
        }

        [Test]
        public void 静止した目標と動いている自機では自機の速度の分を打ち消す方向を狙う()
        {
            var muzzle = Vector3.zero;
            var shooterVel = new Vector3(10, 0, 0);
            var target = new Vector3(0, 0, 100);
            const float speed = 50f;

            Assert.IsTrue(LeadCalculator.TryCalcAimPoint(muzzle, shooterVel, target, Vector3.zero, speed, out var aim));

            // 自機が右へ動いているので、照準点は目標より左になる
            Assert.Less(aim.x, 0f);
            AssertHits(muzzle, shooterVel, target, Vector3.zero, speed, aim);
        }

        [Test]
        public void 等速直線で動く目標には未来の位置を狙って当たる()
        {
            var muzzle = new Vector3(1, 2, 3);
            var shooterVel = new Vector3(0, 0, 5);
            var target = new Vector3(30, 5, 80);
            var targetVel = new Vector3(-8, 0, 3);
            const float speed = 60f;

            Assert.IsTrue(LeadCalculator.TryCalcAimPoint(muzzle, shooterVel, target, targetVel, speed, out var aim));
            AssertHits(muzzle, shooterVel, target, targetVel, speed, aim);
        }

        [Test]
        public void 真横に動く目標には進行方向の先を狙う()
        {
            var target = new Vector3(0, 0, 100);
            var targetVel = new Vector3(30, 0, 0);
            const float speed = 50f;

            Assert.IsTrue(LeadCalculator.TryCalcAimPoint(Vector3.zero, Vector3.zero, target, targetVel, speed, out var aim));

            // |(30t, 0, 100)| = 50t → t = 2.5、照準点は (75, 0, 100)
            AssertVector(new Vector3(75, 0, 100), aim);
        }

        [Test]
        public void 弾が追いつけない目標では失敗して現在位置を返す()
        {
            var target = new Vector3(0, 0, 100);
            var targetVel = new Vector3(0, 0, 80); // 弾より速く遠ざかる

            Assert.IsFalse(LeadCalculator.TryCalcAimPoint(Vector3.zero, Vector3.zero, target, targetVel, 50f, out var aim));
            AssertVector(target, aim);
        }

        [Test]
        public void 相対速度が弾速と等しく遠ざかる目標には追いつけない()
        {
            var target = new Vector3(0, 0, 100);
            var targetVel = new Vector3(0, 0, 50);

            Assert.IsFalse(LeadCalculator.TryCalcAimPoint(Vector3.zero, Vector3.zero, target, targetVel, 50f, out _));
        }

        [Test]
        public void 弾速が0では失敗する()
        {
            Assert.IsFalse(LeadCalculator.TryCalcAimPoint(Vector3.zero, Vector3.zero, Vector3.forward * 10, Vector3.zero, 0f, out _));
        }

        [Test]
        public void 一次ロックでは目標の速度を使わず二次ロックでは使う()
        {
            var muzzle = Vector3.zero;
            var target = new Vector3(0, 0, 100);
            var targetVel = new Vector3(30, 0, 0);
            var fallback = new Vector3(0, 0, 999);
            const float speed = 50f;

            var primary = AimSolver.CalcAimPoint(LockLevel.Primary, muzzle, Vector3.zero, speed, target, targetVel, fallback);
            AssertVector(target, primary);

            var secondary = AimSolver.CalcAimPoint(LockLevel.Secondary, muzzle, Vector3.zero, speed, target, targetVel, fallback);
            AssertVector(new Vector3(75, 0, 100), secondary);

            var none = AimSolver.CalcAimPoint(LockLevel.None, muzzle, Vector3.zero, speed, target, targetVel, fallback);
            AssertVector(fallback, none);
        }

        [Test]
        public void 二次ロックで追いつけないときは目標の現在位置を狙う()
        {
            var target = new Vector3(0, 0, 100);
            var targetVel = new Vector3(0, 0, 80);

            var aim = AimSolver.CalcAimPoint(LockLevel.Secondary, Vector3.zero, Vector3.zero, 50f, target, targetVel, Vector3.zero);
            AssertVector(target, aim);
        }
    }
}
