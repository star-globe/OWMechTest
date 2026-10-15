using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class CharacterParamTests
    {
        PlayerParameterSettings settings;
        CharacterParam param;

        [SetUp]
        public void SetUp()
        {
            // 既定値: maxAp=10000, maxEnergy=10000, energyRecovery=2500, boost=1500, jump=3500, quick=3000, hyper=2000
            settings = ScriptableObject.CreateInstance<PlayerParameterSettings>();
            param = new CharacterParam();
            param.SetInitialInfo(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void SetInitialInfo_CopiesSettings()
        {
            Assert.AreEqual(settings.MaxAP, param.Ap);
            Assert.AreEqual(settings.MaxAP, param.MaxAp);
            Assert.AreEqual(settings.MaxEnergy, param.Energy);
            Assert.AreEqual(settings.JumpEnergy, param.JumpEnergy);
        }

        [Test]
        public void SetDamage_ClampsAtZero()
        {
            param.SetDamage(param.MaxAp + 1);
            Assert.AreEqual(0, param.Ap);
        }

        [Test]
        public void ConsumeJumpEnergy_ReducesEnergy()
        {
            param.ConsumeJumpEnergy();
            Assert.AreEqual(settings.MaxEnergy - settings.JumpEnergy, param.Energy);
        }

        [Test]
        public void ConsumeEnergy_ClampsAtZero()
        {
            param.ConsumeEnergy(param.MaxEnergy * 2);
            Assert.AreEqual(0, param.Energy);
        }

        [Test]
        public void Recovery_ClampsAtMaxEnergy()
        {
            param.ConsumeEnergy(1000);
            param.Recovery(0.2f);   // 2500 * 0.2 = 500
            Assert.AreEqual(param.MaxEnergy - 500, param.Energy);

            param.Recovery(100f);
            Assert.AreEqual(param.MaxEnergy, param.Energy);
        }

        [Test]
        public void CanJump_RequiresMoreEnergyThanCost()
        {
            param.ConsumeEnergy(param.MaxEnergy - param.JumpEnergy);   // 残りエネルギー = JumpEnergy
            Assert.IsFalse(param.CanJump);

            param.Recovery(1f);
            Assert.IsTrue(param.CanJump);
        }
    }

    public class UnitParamTests
    {
        TurretParameterSettings settings;
        UnitParam param;

        [SetUp]
        public void SetUp()
        {
            // 既定値: maxAp=500, fireInterval=2.0, turretRotationSpeedX=30, searchRadius=50
            settings = ScriptableObject.CreateInstance<TurretParameterSettings>();
            param = new UnitParam();
            param.SetInitialInfo(settings);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void SetInitialInfo_CopiesTurretSpecificSettings()
        {
            Assert.AreEqual(settings.MaxAp, param.Ap);
            Assert.AreEqual(settings.TurretRotationSpeedX, param.TurretRotationSpeedX);
            Assert.AreEqual(settings.SearchRadius, param.SearchRadius);
            Assert.AreEqual(0f, param.ForwardSpeed);   // 戦車用の値は設定されない
        }

        [Test]
        public void SetDamage_KillsAtZero()
        {
            param.SetDamage(param.MaxAp - 1);
            Assert.IsTrue(param.IsAlive);

            param.SetDamage(10);
            Assert.AreEqual(0, param.Ap);
            Assert.IsFalse(param.IsAlive);
        }

        [Test]
        public void FireTimer_BlocksUntilIntervalElapsed()
        {
            Assert.IsTrue(param.CanFire);

            param.ResetFireTimer();
            Assert.IsFalse(param.CanFire);

            param.UpdateFireTimer(param.FireInterval * 0.5f);
            Assert.IsFalse(param.CanFire);

            param.UpdateFireTimer(param.FireInterval * 0.5f);
            Assert.IsTrue(param.CanFire);
        }
    }
}
