using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class JumpInfoTests
    {
        [Test]
        public void SetJump_StoresVectorAndResetsTime()
        {
            var info = new JumpInfo();
            info.UpdateJumpSumTime(1.0f);

            info.SetJump(Vector3.up * 5, Vector3.up, 0.3f);

            Assert.IsTrue(info.isJump);
            Assert.AreEqual(0f, info.jumpSumTime);
            Assert.AreEqual(Vector3.up * 5, info.jumpVector);
            Assert.AreEqual(0.3f, info.interval);
        }

        [Test]
        public void ResetJump_ClearsJumpButKeepsInterval()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up, Vector3.up, 0.3f);

            info.ResetJump();

            Assert.IsFalse(info.isJump);
            Assert.AreEqual(Vector3.zero, info.jumpVector);
            Assert.AreEqual(0.3f, info.interval);
        }

        [Test]
        public void ReduceIntervalTime_Subtracts()
        {
            var info = new JumpInfo();
            info.SetJump(Vector3.up, Vector3.up, 0.5f);

            info.ReduceIntervalTime(0.2f);

            Assert.AreEqual(0.3f, info.interval, 1e-5f);
        }
    }

    public class HyperBoostInfoTests
    {
        [Test]
        public void SetBoost_MapsInputToLocalXZ()
        {
            var info = new HyperBoostInfo();

            info.SetBoost(new Vector2(1, 0.5f), 10f);

            Assert.IsTrue(info.isOn);
            Assert.AreEqual(new Vector3(10, 0, 5), info.localBoostVector);
        }

        [Test]
        public void ResetBoost_TurnsOff()
        {
            var info = new HyperBoostInfo();
            info.SetBoost(Vector2.up, 10f);

            info.ResetBoost();

            Assert.IsFalse(info.isOn);
            Assert.AreEqual(Vector3.zero, info.localBoostVector);
        }

        [Test]
        public void CheckInputVector_RejectsOppositeDirection()
        {
            var info = new HyperBoostInfo();
            info.SetBoost(Vector2.up, 10f);

            Assert.IsTrue(info.CheckInputVector(Vector2.up));
            Assert.IsTrue(info.CheckInputVector(Vector2.right));    // 直交は継続
            Assert.IsFalse(info.CheckInputVector(Vector2.down));
        }
    }
}
