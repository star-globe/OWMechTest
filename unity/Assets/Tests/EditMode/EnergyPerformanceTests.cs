using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class EnergyPerformanceTests
    {
        // ── EnergyPerformance ──────────────────────────

        [Test]
        public void Calc_RecoveryIsOutputMinusConstantConsumption()
        {
            var p = EnergyPerformance.Calc(10000, 5000, new[] { 300, 500, 300, 400 }, 1500);

            Assert.AreEqual(10000, p.MaxEnergy);
            Assert.AreEqual(3500, p.RecoveryBoostOff);
            Assert.AreEqual(2000, p.RecoveryBoostOn);
            Assert.IsTrue(p.IsValid);
        }

        [Test]
        public void Calc_SameWeaponOnBothHandsCountsTwice()
        {
            // 左右に同じ武器（常時消費 200）を持つと 2 本分を消費する
            var p = EnergyPerformance.Calc(10000, 5000, new[] { 1500, 200, 200 }, 0);

            Assert.AreEqual(3100, p.RecoveryBoostOff);
        }

        [Test]
        public void Calc_NoParts_RecoveryEqualsOutput()
        {
            var p = EnergyPerformance.Calc(8000, 4000, null, 1000);

            Assert.AreEqual(4000, p.RecoveryBoostOff);
            Assert.AreEqual(3000, p.RecoveryBoostOn);
        }

        [Test]
        public void IsValid_FalseWhenBoostOnRecoveryIsNotPositive()
        {
            // OFF では回復するが、ON でちょうど 0 になる組み合わせは組めない
            var p = EnergyPerformance.Calc(10000, 3000, new[] { 1500 }, 1500);

            Assert.AreEqual(1500, p.RecoveryBoostOff);
            Assert.AreEqual(0, p.RecoveryBoostOn);
            Assert.IsFalse(p.IsValid);
        }

        [Test]
        public void IsValid_FalseWhenBoostOffRecoveryIsNotPositive()
        {
            var p = EnergyPerformance.Calc(10000, 1000, new[] { 1200 }, 0);

            Assert.IsFalse(p.IsValid);
        }

        [Test]
        public void GetRecovery_SwitchesByBoostState()
        {
            var p = new EnergyPerformance(10000, 3500, 2000);

            Assert.AreEqual(3500, p.GetRecovery(false));
            Assert.AreEqual(2000, p.GetRecovery(true));
        }

        [Test]
        public void FromFixedValues_BoostOnLowersRecovery()
        {
            var p = EnergyPerformance.FromFixedValues(10000, 3500, 1500);

            Assert.AreEqual(10000, p.MaxEnergy);
            Assert.AreEqual(3500, p.RecoveryBoostOff);
            Assert.AreEqual(2000, p.RecoveryBoostOn);
        }

        // ── CharacterParam ─────────────────────────────

        [Test]
        public void CharacterParam_RecoveryFollowsBoostState()
        {
            var param = new CharacterParam();
            param.SetEnergyPerformance(new EnergyPerformance(10000, 3000, 1000));
            param.ConsumeEnergy(10000);

            param.Recovery(1f);
            Assert.AreEqual(3000, param.Energy);

            param.SetBoost(true);
            param.Recovery(1f);
            Assert.AreEqual(4000, param.Energy);
        }

        [Test]
        public void CharacterParam_RecoveryDoesNotExceedMax()
        {
            var param = new CharacterParam();
            param.SetEnergyPerformance(new EnergyPerformance(5000, 3000, 1000));
            param.ConsumeEnergy(1000);

            param.Recovery(1f);
            Assert.AreEqual(5000, param.Energy);
        }

        [Test]
        public void CharacterParam_SetEnergyPerformanceFillsEnergy()
        {
            var param = new CharacterParam();
            param.SetEnergyPerformance(new EnergyPerformance(7000, 3000, 1000));

            Assert.AreEqual(7000, param.MaxEnergy);
            Assert.AreEqual(7000, param.Energy);
        }

        // ── NpcDecider（ブースト） ────────────────────────────

        [Test]
        public void NpcBoost_OnTurnsOffAtTenPercentOrLess()
        {
            Assert.IsFalse(NpcDecider.ShouldToggleBoost(true, 1001, 10000));
            Assert.IsTrue(NpcDecider.ShouldToggleBoost(true, 1000, 10000));
            Assert.IsTrue(NpcDecider.ShouldToggleBoost(true, 0, 10000));
        }

        [Test]
        public void NpcBoost_OffTurnsOnAtFiftyPercentOrMore()
        {
            Assert.IsFalse(NpcDecider.ShouldToggleBoost(false, 4999, 10000));
            Assert.IsTrue(NpcDecider.ShouldToggleBoost(false, 5000, 10000));
            Assert.IsTrue(NpcDecider.ShouldToggleBoost(false, 10000, 10000));
        }

        [Test]
        public void NpcBoost_BetweenThresholdsKeepsState()
        {
            // 10% より多く 50% 未満のときは、どちらの状態でも切り替えない（毎フレームの切り替えを防ぐ）
            Assert.IsFalse(NpcDecider.ShouldToggleBoost(true, 3000, 10000));
            Assert.IsFalse(NpcDecider.ShouldToggleBoost(false, 3000, 10000));
        }

        [Test]
        public void NpcBoost_ZeroMaxEnergyStaysOff()
        {
            Assert.IsTrue(NpcDecider.ShouldToggleBoost(true, 0, 0));
            Assert.IsFalse(NpcDecider.ShouldToggleBoost(false, 0, 0));
        }
    }
}
