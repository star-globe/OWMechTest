using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class FollowCameraTests
    {
        [Test]
        public void 距離と見下ろす角度からカメラの位置を求める()
        {
            var offset = FollowCamera.CalcCameraLocalOffset(10f, 30f);
            Assert.AreEqual(0f, offset.x, 1e-4f);
            Assert.AreEqual(5f, offset.y, 1e-4f);
            Assert.AreEqual(-10f * Mathf.Cos(30f * Mathf.Deg2Rad), offset.z, 1e-4f);
            Assert.AreEqual(10f, offset.magnitude, 1e-4f);
        }

        [Test]
        public void 角度が0ならプレイヤーの真後ろに置く()
        {
            var offset = FollowCamera.CalcCameraLocalOffset(8f, 0f);
            Assert.AreEqual(0f, offset.y, 1e-4f);
            Assert.AreEqual(-8f, offset.z, 1e-4f);
        }

        [Test]
        public void 初期値は以前のカメラ位置と同じになる()
        {
            // 改修前は camTarget の localPosition (0, 6, -16) を使っていた
            var offset = FollowCamera.CalcCameraLocalOffset(17.088f, 20.556f);
            Assert.AreEqual(6f, offset.y, 1e-2f);
            Assert.AreEqual(-16f, offset.z, 1e-2f);
        }

        [Test]
        public void 遊びの角度を距離に換算する()
        {
            Assert.AreEqual(10f, FollowCamera.CalcDeadZoneDistance(10f, 45f), 1e-4f);
            Assert.AreEqual(0f, FollowCamera.CalcDeadZoneDistance(10f, 0f), 1e-6f);
        }

        [Test]
        public void 遊びは距離に比例する()
        {
            var near = FollowCamera.CalcDeadZoneDistance(10f, 2f);
            var far = FollowCamera.CalcDeadZoneDistance(20f, 2f);
            Assert.AreEqual(near * 2f, far, 1e-4f);
        }

        [Test]
        public void 遊びの初期値は以前の値とほぼ同じになる()
        {
            // 改修前の cameraDiffMin は 0.3 m
            Assert.AreEqual(0.3f, FollowCamera.CalcDeadZoneDistance(17.088f, 1f), 0.01f);
        }

        [Test]
        public void 負の角度や距離では遊びは0になる()
        {
            Assert.AreEqual(0f, FollowCamera.CalcDeadZoneDistance(10f, -5f), 1e-6f);
            Assert.AreEqual(0f, FollowCamera.CalcDeadZoneDistance(-10f, 5f), 1e-6f);
        }
    }
}
