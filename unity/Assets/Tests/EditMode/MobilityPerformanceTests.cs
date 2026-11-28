using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class MobilityPerformanceTests
    {
        static readonly MobilityConst Const = new MobilityConst(2.0f, 0.2f);

        // ── 重量補正 ──────────────────────────

        [Test]
        public void CalcWeightRate_WithinCapacity_IsOne()
        {
            Assert.AreEqual(1f, MobilityPerformance.CalcWeightRate(0, 5000, Const));
            Assert.AreEqual(1f, MobilityPerformance.CalcWeightRate(4000, 5000, Const));
        }

        [Test]
        public void CalcWeightRate_ExactlyCapacity_IsOne()
        {
            Assert.AreEqual(1f, MobilityPerformance.CalcWeightRate(5000, 5000, Const));
        }

        [Test]
        public void CalcWeightRate_Over10Percent_DecreasesByOverweightRate()
        {
            // 1 − 2.0 × (1.1 − 1) = 0.8
            Assert.AreEqual(0.8f, MobilityPerformance.CalcWeightRate(5500, 5000, Const), 1e-5f);
        }

        [Test]
        public void CalcWeightRate_FarOver_ClampedToMin()
        {
            Assert.AreEqual(0.2f, MobilityPerformance.CalcWeightRate(10000, 5000, Const), 1e-5f);
        }

        [Test]
        public void CalcWeightRate_ZeroCapacityWithWeight_IsMin()
        {
            Assert.AreEqual(0.2f, MobilityPerformance.CalcWeightRate(100, 0, Const), 1e-5f);
        }

        [Test]
        public void CalcWeightRate_ZeroCapacityWithoutWeight_IsOne()
        {
            Assert.AreEqual(1f, MobilityPerformance.CalcWeightRate(0, 0, Const));
        }

        // ── スピード ──────────────────────────

        [Test]
        public void Speed_WithinCapacity_IsWalkSpeed()
        {
            var p = new MobilityPerformance(4000, 5000, 9f, Const);

            Assert.IsFalse(p.IsOverweight);
            Assert.AreEqual(9f, p.Speed, 1e-5f);
        }

        [Test]
        public void Speed_Overweight_WalkSpeedTimesWeightRate()
        {
            var p = new MobilityPerformance(5500, 5000, 10f, Const);

            Assert.IsTrue(p.IsOverweight);
            Assert.AreEqual(8f, p.Speed, 1e-5f);
        }

        [Test]
        public void GetBoostSpeed_IsSpeedTimesBoostRate()
        {
            var p = new MobilityPerformance(5500, 5000, 10f, Const);

            Assert.AreEqual(16f, p.GetBoostSpeed(2f), 1e-5f);
        }

        // ── 移動のパラメーターへの反映 ──────────────────────────

        [Test]
        public void PlayerMoveParams_WithMobility_SetsSpeedAndScalesQuickAndHyper()
        {
            var original = new PlayerMoveParams
            {
                VecSpeed = 9f,
                HorizontalBoostRate = 2f,
                QuickSpeed = 15f,
                QuickTime = 0.1f,
                HyperBoostSpeed = 30f,
                JumpSpeed = 8f,
            };
            var mobility = new MobilityPerformance(5500, 5000, 10f, Const);

            var p = original.WithMobility(mobility);

            Assert.AreEqual(8f, p.VecSpeed, 1e-5f);
            Assert.AreEqual(12f, p.QuickSpeed, 1e-5f);
            Assert.AreEqual(24f, p.HyperBoostSpeed, 1e-5f);
            Assert.AreEqual(2f, p.HorizontalBoostRate);
            Assert.AreEqual(0.1f, p.QuickTime);
            Assert.AreEqual(8f, p.JumpSpeed);
        }

        [Test]
        public void LimitSpeed_WithMobility_BoostOnIsBodySpeedTimesRate()
        {
            var original = new PlayerMoveParams { VecSpeed = 9f, HorizontalBoostRate = 2f, HyperBoostSpeed = 30f };
            var p = original.WithMobility(new MobilityPerformance(5500, 5000, 10f, Const));

            Assert.AreEqual(8f, PlayerMoveCalculator.LimitSpeed(p, false, false), 1e-5f);
            Assert.AreEqual(16f, PlayerMoveCalculator.LimitSpeed(p, true, false), 1e-5f);
            Assert.AreEqual(24f, PlayerMoveCalculator.LimitSpeed(p, true, true), 1e-5f);
        }

        // ── 装備から引く ──────────────────────────

        [Test]
        public void Resolver_UnequippedLeg_ReturnsFalse()
        {
            Assert.IsFalse(MobilityPerformanceResolver.TryResolve(_ => -1, Const, out _));
        }

        [Test]
        public void Resolver_UnknownLegId_ReturnsFalse()
        {
            Assert.IsFalse(MobilityPerformanceResolver.TryResolve(_ => int.MaxValue, Const, out _));
        }

        [Test]
        public void WeightSlots_ExcludeLeg()
        {
            CollectionAssert.DoesNotContain(MobilityPerformanceResolver.WeightSlots, PartsAttachType.Leg);
            CollectionAssert.Contains(MobilityPerformanceResolver.WeightSlots, PartsAttachType.Weapon_Right);
            CollectionAssert.Contains(MobilityPerformanceResolver.WeightSlots, PartsAttachType.Weapon_Left);
        }

        // ── カスタマイズ画面の文言 ──────────────────────────

        [Test]
        public void FormatPerformance_ShowsWeightAndSpeeds()
        {
            var text = CustomizeMobilityText.FormatPerformance(new MobilityPerformance(4000, 5000, 9f, Const), 2f);

            StringAssert.Contains("総重量: 4000 / 積載量: 5000", text);
            StringAssert.Contains("スピード（ブースト OFF）: 9.0", text);
            StringAssert.Contains("スピード（ブースト ON）: 18.0", text);
        }

        [Test]
        public void FormatPerformance_NoBooster_ShowsStandard()
        {
            var text = CustomizeMobilityText.FormatPerformance(new MobilityPerformance(4000, 5000, 9f, Const), null);

            StringAssert.Contains(CustomizeMobilityText.NoBoosterText, text);
        }

        [Test]
        public void GetOverweightWarning_WithinCapacity_Null()
        {
            Assert.IsNull(CustomizeMobilityText.GetOverweightWarning(new MobilityPerformance(5000, 5000, 9f, Const)));
        }

        [Test]
        public void GetOverweightWarning_Overweight_ContainsRate()
        {
            var warning = CustomizeMobilityText.GetOverweightWarning(new MobilityPerformance(5500, 5000, 9f, Const));

            StringAssert.Contains("5500 / 5000", warning);
            StringAssert.Contains("×0.80", warning);
        }
    }
}
