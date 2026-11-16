using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class BoosterEffectCheckStateTests
    {
        [Test]
        public void AllDirectionsBit_CoversEveryBoostVector()
        {
            foreach (BoostVector vec in System.Enum.GetValues(typeof(BoostVector)))
                Assert.AreNotEqual(0, BoosterEffectCheckState.AllDirectionsBit & (1 << (byte)vec), vec.ToString());
        }

        [Test]
        public void Initial_AllOffAndPartColor()
        {
            var state = new BoosterEffectCheckState(3);

            Assert.IsFalse(state.IsBoostOn);
            Assert.IsFalse(state.IsHyperOn);
            Assert.AreEqual(0, state.BoostBit);
            Assert.AreEqual(0, state.HyperBit);
            Assert.IsTrue(state.IsPartColor);
        }

        [Test]
        public void ToggleBoost_SwitchesBoostBit()
        {
            var state = new BoosterEffectCheckState(0);

            state.ToggleBoost();
            Assert.AreEqual(BoosterEffectCheckState.AllDirectionsBit, state.BoostBit);
            Assert.AreEqual(0, state.HyperBit, "ハイパーは別に切り替える");

            state.ToggleBoost();
            Assert.AreEqual(0, state.BoostBit);
        }

        [Test]
        public void ToggleHyper_SwitchesHyperBit()
        {
            var state = new BoosterEffectCheckState(0);

            state.ToggleHyper();
            Assert.AreEqual(BoosterEffectCheckState.AllDirectionsBit, state.HyperBit);
            Assert.AreEqual(0, state.BoostBit);
        }

        [Test]
        public void QuickBit_AlwaysAllDirections()
        {
            Assert.AreEqual(BoosterEffectCheckState.AllDirectionsBit, new BoosterEffectCheckState(0).QuickBit);
        }

        [Test]
        public void ResetBoost_StopsBoostButKeepsColor()
        {
            var state = new BoosterEffectCheckState(2);
            state.ToggleBoost();
            state.ToggleHyper();
            state.NextColor();

            state.ResetBoost();

            Assert.IsFalse(state.IsBoostOn);
            Assert.IsFalse(state.IsHyperOn);
            Assert.AreEqual(0, state.ColorIndex);
        }

        [Test]
        public void NextColor_CyclesPartColorThenPresets()
        {
            var state = new BoosterEffectCheckState(2);

            state.NextColor();
            Assert.AreEqual(0, state.ColorIndex);
            state.NextColor();
            Assert.AreEqual(1, state.ColorIndex);
            state.NextColor();
            Assert.AreEqual(BoosterEffectCheckState.PartColorIndex, state.ColorIndex);
        }

        [Test]
        public void NextColor_NoPresets_StaysPartColor()
        {
            var state = new BoosterEffectCheckState(0);

            state.NextColor();

            Assert.IsTrue(state.IsPartColor);
        }
    }
}
