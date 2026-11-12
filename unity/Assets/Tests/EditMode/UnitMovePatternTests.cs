using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class UnitMovePatternTests
    {
        const float Tolerance = 1e-3f;

        static void AssertVector(Vector3 expected, Vector3 actual, string message = "")
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, message + " x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, message + " y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, message + " z");
        }

        /// <summary>位置の数値微分が速度と一致するか</summary>
        static void AssertVelocityMatchesPosition(IUnitMovePattern pattern, float time)
        {
            const float h = 1e-3f;
            pattern.Evaluate(time - h, out var before, out _);
            pattern.Evaluate(time + h, out var after, out _);
            pattern.Evaluate(time, out _, out var velocity);

            var numerical = (after - before) / (2f * h);
            Assert.AreEqual(numerical.x, velocity.x, 0.05f, $"t={time} vx");
            Assert.AreEqual(numerical.y, velocity.y, 0.05f, $"t={time} vy");
            Assert.AreEqual(numerical.z, velocity.z, 0.05f, $"t={time} vz");
        }

        // ── 円運動 ───────────────────────────────────────

        [Test]
        public void 水平の円運動は中心から半径の距離を保ち周速で動く()
        {
            var center = new Vector3(0f, 2f, 10f);
            var pattern = new CircularMovePattern(center, 10f, 5f, CircleDirection.Forward, 0f, CirclePlane.Horizontal, 0f);

            for (float t = 0f; t < 20f; t += 0.7f)
            {
                pattern.Evaluate(t, out var pos, out var vel);
                Assert.AreEqual(10f, (pos - center).magnitude, Tolerance, $"t={t} 半径");
                Assert.AreEqual(center.y, pos.y, Tolerance, $"t={t} 高さ");
                Assert.AreEqual(5f, vel.magnitude, Tolerance, $"t={t} 周速");
                Assert.AreEqual(0f, vel.y, Tolerance, $"t={t} 縦の速度");
                AssertVelocityMatchesPosition(pattern, t);
            }
        }

        [Test]
        public void 水平の円運動のForwardは上から見て時計回り()
        {
            // 半径 10・周速 10 → 角速度 1 rad/s。π/2 秒で正面（+Z）から右（+X）へ
            var pattern = new CircularMovePattern(Vector3.zero, 10f, 10f, CircleDirection.Forward, 0f, CirclePlane.Horizontal, 0f);
            pattern.Evaluate(0f, out var start, out var startVel);
            pattern.Evaluate(Mathf.PI / 2f, out var quarter, out _);

            AssertVector(new Vector3(0f, 0f, 10f), start);
            AssertVector(new Vector3(10f, 0f, 0f), startVel);
            AssertVector(new Vector3(10f, 0f, 0f), quarter);
            Assert.IsFalse(pattern.HasFixedForward);
        }

        [Test]
        public void 水平の円運動のReverseは逆回り()
        {
            var pattern = new CircularMovePattern(Vector3.zero, 10f, 10f, CircleDirection.Reverse, 0f, CirclePlane.Horizontal, 0f);
            pattern.Evaluate(Mathf.PI / 2f, out var quarter, out _);
            AssertVector(new Vector3(-10f, 0f, 0f), quarter);
        }

        [Test]
        public void 円運動は1周すると同じ位置に戻る()
        {
            var pattern = new CircularMovePattern(new Vector3(3f, 0f, 5f), 8f, 4f, CircleDirection.Forward, 30f, CirclePlane.Horizontal, 0f);
            var period = 2f * Mathf.PI * 8f / 4f;
            pattern.Evaluate(1f, out var a, out _);
            pattern.Evaluate(1f + period, out var b, out _);
            AssertVector(a, b);
        }

        [Test]
        public void 縦の円運動は面の前方向から上へ回り向きを固定する()
        {
            // 面の向き 90 度 → 面はスポーン時の右（+X）と上を含む
            var pattern = new CircularMovePattern(Vector3.zero, 10f, 10f, CircleDirection.Forward, 0f, CirclePlane.Vertical, 90f);
            pattern.Evaluate(0f, out var start, out var startVel);
            pattern.Evaluate(Mathf.PI / 2f, out var quarter, out _);

            AssertVector(new Vector3(10f, 0f, 0f), start);
            AssertVector(new Vector3(0f, 10f, 0f), startVel);
            AssertVector(new Vector3(0f, 10f, 0f), quarter);

            Assert.IsTrue(pattern.HasFixedForward);
            AssertVector(new Vector3(1f, 0f, 0f), pattern.FixedForward);

            for (float t = 0f; t < 10f; t += 0.9f)
            {
                pattern.Evaluate(t, out var pos, out _);
                Assert.AreEqual(0f, pos.z, Tolerance, $"t={t} 面の外へ出ない");
                AssertVelocityMatchesPosition(pattern, t);
            }
        }

        [Test]
        public void 半径0の円運動は中心に止まる()
        {
            var center = new Vector3(1f, 2f, 3f);
            var pattern = new CircularMovePattern(center, 0f, 5f, CircleDirection.Forward, 0f, CirclePlane.Horizontal, 0f);
            pattern.Evaluate(3f, out var pos, out var vel);
            AssertVector(center, pos);
            AssertVector(Vector3.zero, vel);
        }

        // ── 停止と直進 ───────────────────────────────────

        // 最高速度 10・加速度 5・減速度 10・直進 5 秒・停止 1 秒
        // → 加速 2 秒（10m）、等速 2 秒（20m）、減速 1 秒（5m）、1 回の直進は 35m、周期 6 秒
        static StopAndGoMovePattern CreateStopAndGo(float turnAngle)
            => new StopAndGoMovePattern(10f, 5f, 10f, 5f, 1f, turnAngle);

        [Test]
        public void 停止と直進は停止から始まり加速して最高速度に達する()
        {
            var pattern = CreateStopAndGo(180f);
            Assert.AreEqual(35f, pattern.LegDistance, Tolerance);
            Assert.AreEqual(6f, pattern.CycleTime, Tolerance);

            pattern.Evaluate(0.5f, out var stopPos, out var stopVel);
            AssertVector(Vector3.zero, stopPos, "停止中");
            AssertVector(Vector3.zero, stopVel, "停止中");

            // 加速中（直進を始めて 1 秒）
            pattern.Evaluate(2f, out var accelPos, out var accelVel);
            AssertVector(new Vector3(0f, 0f, 2.5f), accelPos, "加速中");
            AssertVector(new Vector3(0f, 0f, 5f), accelVel, "加速中");

            // 等速（直進を始めて 3 秒）
            pattern.Evaluate(4f, out var cruisePos, out var cruiseVel);
            AssertVector(new Vector3(0f, 0f, 20f), cruisePos, "等速");
            AssertVector(new Vector3(0f, 0f, 10f), cruiseVel, "等速");
        }

        [Test]
        public void 停止と直進は減速して止まる()
        {
            var pattern = CreateStopAndGo(180f);

            // 減速中（直進を始めて 4.5 秒、減速を始めて 0.5 秒）
            pattern.Evaluate(5.5f, out var decelPos, out var decelVel);
            AssertVector(new Vector3(0f, 0f, 30f + 5f - 1.25f), decelPos, "減速中");
            AssertVector(new Vector3(0f, 0f, 5f), decelVel, "減速中");

            // 次の停止（周期の境目の直後）
            pattern.Evaluate(6.5f, out var stopPos, out var stopVel);
            AssertVector(new Vector3(0f, 0f, 35f), stopPos, "停止中");
            AssertVector(Vector3.zero, stopVel, "停止中");
        }

        [Test]
        public void 停止と直進の速度は位置の変化と一致する()
        {
            var pattern = CreateStopAndGo(90f);
            for (float t = 0.25f; t < 30f; t += 0.37f)
                AssertVelocityMatchesPosition(pattern, t);
        }

        [Test]
        public void 往復は2回の直進で元の位置に戻る()
        {
            var pattern = CreateStopAndGo(180f);

            pattern.Evaluate(9f, out _, out var backVel);
            Assert.Less(backVel.z, 0f, "2 回目は逆向き");

            pattern.Evaluate(12.5f, out var back, out _);
            AssertVector(Vector3.zero, back);

            // 何周しても元の位置に戻る
            pattern.Evaluate(12f * 100f + 0.5f, out var later, out _);
            AssertVector(Vector3.zero, later);
        }

        [Test]
        public void 直角に曲がると四角形を描いて元の位置に戻る()
        {
            var pattern = CreateStopAndGo(90f);

            // 各直進の終わり（次の停止中）の位置：前 → 右 → 後ろ → 左
            var corners = new[]
            {
                new Vector3(0f, 0f, 35f),
                new Vector3(35f, 0f, 35f),
                new Vector3(35f, 0f, 0f),
                Vector3.zero,
            };

            for (int i = 0; i < corners.Length; i++)
            {
                pattern.Evaluate(6f * (i + 1) + 0.5f, out var pos, out _);
                AssertVector(corners[i], pos, $"{i + 1} 回目の直進の後");
            }

            pattern.Evaluate(24f * 50f + 0.5f, out var later, out _);
            AssertVector(Vector3.zero, later, "50 周後");
        }

        [Test]
        public void 加速度と減速度が0なら即座に最高速度になり即座に止まる()
        {
            var pattern = new StopAndGoMovePattern(10f, 0f, 0f, 2f, 1f, 180f);
            Assert.AreEqual(20f, pattern.LegDistance, Tolerance);

            pattern.Evaluate(1.01f, out _, out var startVel);
            AssertVector(new Vector3(0f, 0f, 10f), startVel);

            pattern.Evaluate(2.99f, out _, out var endVel);
            AssertVector(new Vector3(0f, 0f, 10f), endVel);
        }

        [Test]
        public void 直進時間が短くて最高速度に届かないときは途中で減速する()
        {
            // 加速度 5・減速度 5・直進 2 秒 → 1 秒加速して 5m/s、1 秒減速
            var pattern = new StopAndGoMovePattern(10f, 5f, 5f, 2f, 0f, 180f);
            Assert.AreEqual(5f, pattern.LegDistance, Tolerance);

            pattern.Evaluate(1f, out _, out var peakVel);
            AssertVector(new Vector3(0f, 0f, 5f), peakVel);
        }

        [Test]
        public void 同じ設定なら同じ動きになる()
        {
            var a = CreateStopAndGo(90f);
            var b = CreateStopAndGo(90f);
            for (float t = 0f; t < 50f; t += 1.3f)
            {
                a.Evaluate(t, out var posA, out var velA);
                b.Evaluate(t, out var posB, out var velB);
                AssertVector(posA, posB);
                AssertVector(velA, velB);
            }
        }
    }
}
