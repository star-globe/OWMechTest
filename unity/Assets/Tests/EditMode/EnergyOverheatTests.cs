using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class EnergyOverheatTests
    {
        CharacterParam param;

        [SetUp]
        public void SetUp()
        {
            // 最大 10000、回復力 OFF 3000 / ON 1000、オーバーヒート時間 2 秒、復帰エネルギー 40%（4000）
            param = new CharacterParam();
            param.SetEnergyPerformance(new EnergyPerformance(10000, 3000, 1000, 2f, 0.4f));
        }

        // ── EnergyPerformance ──────────────────────────

        [Test]
        public void Performance_DefaultOverheatValues()
        {
            var p = new EnergyPerformance(10000, 3500, 2000);

            Assert.AreEqual(EnergyPerformance.DefaultOverheatTime, p.OverheatTime);
            Assert.AreEqual(3000, p.OverheatRecoveryEnergy);   // 10000 × 30%
        }

        [Test]
        public void Performance_CalcCarriesGeneratorOverheatValues()
        {
            var p = EnergyPerformance.Calc(8000, 4000, new[] { 1000 }, 500, 5f, 0.25f);

            Assert.AreEqual(5f, p.OverheatTime);
            Assert.AreEqual(0.25f, p.OverheatRecoveryRate);
            Assert.AreEqual(2000, p.OverheatRecoveryEnergy);
        }

        [Test]
        public void Performance_FromFixedValuesCarriesOverheatValues()
        {
            var p = EnergyPerformance.FromFixedValues(10000, 3500, 1500, 4f, 0.5f);

            Assert.AreEqual(4f, p.OverheatTime);
            Assert.AreEqual(5000, p.OverheatRecoveryEnergy);
        }

        [Test]
        public void Performance_ClampsInvalidOverheatValues()
        {
            var p = new EnergyPerformance(10000, 3500, 2000, -1f, 1.5f);

            Assert.AreEqual(0f, p.OverheatTime);
            Assert.AreEqual(10000, p.OverheatRecoveryEnergy);
        }

        // ── CharacterParam ─────────────────────────────

        [Test]
        public void ExactlyZero_DoesNotOverheat()
        {
            param.ConsumeEnergy(10000);

            Assert.AreEqual(0, param.Energy);
            Assert.IsFalse(param.IsOverheat);
            Assert.IsTrue(param.CanUseEnergy);
        }

        [Test]
        public void BelowZero_OverheatsAtZero()
        {
            param.ConsumeEnergy(9000);
            Assert.IsTrue(param.CanQuick);      // 残量 1000 でも行動できる

            param.ConsumeEnergy(2500);          // 残量がマイナスに達する

            Assert.AreEqual(0, param.Energy);
            Assert.IsTrue(param.IsOverheat);
            Assert.IsFalse(param.CanJump);
            Assert.IsFalse(param.CanQuick);
            Assert.IsFalse(param.CanHyperBoost);
            Assert.IsFalse(param.CanAscend);
            Assert.IsFalse(param.HasEnergyFor(0));
        }

        [Test]
        public void Overheat_IgnoresConsumption()
        {
            param.ConsumeEnergy(10001);
            param.Recovery(1f);     // 2000 まで増える
            param.ConsumeEnergy(1000);

            Assert.AreEqual(2000, param.Energy);
        }

        [Test]
        public void Overheat_EnergyRisesLinearlyToRecoveryEnergy()
        {
            param.ConsumeEnergy(10001);

            // 通常の回復（3000 / 秒）ではなく、2 秒で 4000 まで一定の速さで増える
            param.Recovery(0.5f);
            Assert.AreEqual(1000, param.Energy);
            Assert.IsTrue(param.IsOverheat);

            param.Recovery(1f);
            Assert.AreEqual(3000, param.Energy);
            Assert.IsTrue(param.IsOverheat);
            Assert.AreEqual(1.5f, param.OverheatElapsed, 1e-4f);
        }

        [Test]
        public void Overheat_ReleasedWithRecoveryEnergy()
        {
            param.ConsumeEnergy(10001);

            param.Recovery(2f);

            Assert.IsFalse(param.IsOverheat);
            Assert.AreEqual(4000, param.Energy);
            Assert.AreEqual(0f, param.OverheatElapsed);
            Assert.IsTrue(param.CanJump);
        }

        [Test]
        public void Overheat_RecoversNormallyAfterRelease()
        {
            param.ConsumeEnergy(10001);
            param.Recovery(2f);

            param.Recovery(1f);

            Assert.AreEqual(7000, param.Energy);
        }

        [Test]
        public void Overheat_ReleasedImmediatelyWhenTimeIsZero()
        {
            param.SetEnergyPerformance(new EnergyPerformance(10000, 3000, 1000, 0f, 0.3f));
            param.ConsumeEnergy(10001);
            Assert.IsTrue(param.IsOverheat);

            param.Recovery(0.01f);

            Assert.IsFalse(param.IsOverheat);
            Assert.AreEqual(3000, param.Energy);
        }

        [Test]
        public void SetEnergyPerformance_ClearsOverheat()
        {
            param.ConsumeEnergy(10001);

            param.SetEnergyPerformance(new EnergyPerformance(10000, 3000, 1000));

            Assert.IsFalse(param.IsOverheat);
            Assert.AreEqual(10000, param.Energy);
        }

        // ── EnergyDisplay ──────────────────────────────

        [Test]
        public void BlinkRate_StartsRedAndCycles()
        {
            Assert.AreEqual(1f, EnergyDisplay.CalcOverheatBlinkRate(0f, 0.5f), 1e-4f);
            Assert.AreEqual(0f, EnergyDisplay.CalcOverheatBlinkRate(0.25f, 0.5f), 1e-4f);
            Assert.AreEqual(1f, EnergyDisplay.CalcOverheatBlinkRate(0.5f, 0.5f), 1e-4f);
        }

        [Test]
        public void BlinkRate_ZeroPeriodStaysRed()
        {
            Assert.AreEqual(1f, EnergyDisplay.CalcOverheatBlinkRate(0.3f, 0f));
        }
    }
}
