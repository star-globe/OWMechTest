using NUnit.Framework;

namespace AdvancedGears.Tests
{
    /// <summary>NPC の行動の判断（NpcDecider）と旋回の入力（NpcController.CalcTurnInput）</summary>
    public class NpcDeciderTests
    {
        const float AscendHeight = 15f;

        static NpcDecisionContext Context(bool isBoost = false, int energy = 10000, int maxEnergy = 10000,
                                          bool isFloating = false, bool hasLockTarget = false,
                                          float? heightToDestination = null)
        {
            return new NpcDecisionContext(isBoost, energy, maxEnergy, isFloating, hasLockTarget, heightToDestination);
        }

        [Test]
        public void ToggleBoost_UsesEnergyRate()
        {
            var decider = new NpcDecider(AscendHeight);
            Assert.IsTrue(decider.ShouldToggleBoost(Context(isBoost: true, energy: 1000)));
            Assert.IsFalse(decider.ShouldToggleBoost(Context(isBoost: true, energy: 3000)));
            Assert.IsTrue(decider.ShouldToggleBoost(Context(isBoost: false, energy: 5000)));
        }

        [Test]
        public void JumpQuickHyperBoost_AreNotUsedEvenWithFullEnergy()
        {
            // 当面は使わない（接地するたびにジャンプしていた問題の対策）
            var decider = new NpcDecider(AscendHeight);
            var ctx = Context(isBoost: true, energy: 10000);
            Assert.IsFalse(decider.ShouldJump(ctx));
            Assert.IsFalse(decider.ShouldQuick(ctx));
            Assert.IsFalse(decider.ShouldHyperBoost(ctx));
        }

        [Test]
        public void Ascend_OnlyWhenDestinationIsHighEnough()
        {
            var decider = new NpcDecider(AscendHeight);
            Assert.IsFalse(decider.ShouldAscend(Context(heightToDestination: null)));
            Assert.IsFalse(decider.ShouldAscend(Context(heightToDestination: 14.9f)));
            Assert.IsTrue(decider.ShouldAscend(Context(heightToDestination: 15f)));
        }

        [Test]
        public void Fire_OnlyWhileLocked()
        {
            var decider = new NpcDecider(AscendHeight);
            Assert.IsFalse(decider.ShouldFire(WeaponSlot.Right, Context(hasLockTarget: false)));
            Assert.IsTrue(decider.ShouldFire(WeaponSlot.Right, Context(hasLockTarget: true)));
            Assert.IsTrue(decider.ShouldFire(WeaponSlot.Left, Context(hasLockTarget: true)));
        }

        [Test]
        public void TurnInput_ScalesWithAngleAndClamps()
        {
            Assert.AreEqual(0f, NpcController.CalcTurnInput(0f, 45f), 1e-5f);
            Assert.AreEqual(0.5f, NpcController.CalcTurnInput(22.5f, 45f), 1e-5f);
            Assert.AreEqual(-1f, NpcController.CalcTurnInput(-90f, 45f), 1e-5f);
            Assert.AreEqual(1f, NpcController.CalcTurnInput(180f, 45f), 1e-5f);
        }

        [Test]
        public void TurnInput_ZeroFullAngleIsSignOnly()
        {
            Assert.AreEqual(1f, NpcController.CalcTurnInput(3f, 0f));
            Assert.AreEqual(-1f, NpcController.CalcTurnInput(-3f, 0f));
            Assert.AreEqual(0f, NpcController.CalcTurnInput(0f, 0f));
        }
    }
}
