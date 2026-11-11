using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class LockOnStateTests
    {
        [Test]
        public void 目標がなければロックしていない()
        {
            var state = new LockOnState();
            state.AddTime(10f);

            Assert.IsFalse(state.HasTarget);
            Assert.AreEqual(0f, state.Duration);
            Assert.AreEqual(LockLevel.None, state.GetLevel(1f));
        }

        [Test]
        public void 目標を設定すると一次ロックになり時間が経つと二次ロックになる()
        {
            var state = new LockOnState();
            state.SetTarget(5L);
            Assert.AreEqual(LockLevel.Primary, state.GetLevel(1f));

            state.AddTime(0.6f);
            Assert.AreEqual(LockLevel.Primary, state.GetLevel(1f));

            state.AddTime(0.6f);
            Assert.AreEqual(LockLevel.Secondary, state.GetLevel(1f));
        }

        [Test]
        public void 二次ロックまでの時間は武器ごとに渡す()
        {
            var state = new LockOnState();
            state.SetTarget(5L);
            state.AddTime(1.5f);

            Assert.AreEqual(LockLevel.Secondary, state.GetLevel(1f));
            Assert.AreEqual(LockLevel.Primary, state.GetLevel(2f));
            Assert.AreEqual(LockLevel.Primary, state.GetLevel(SecondaryLockTime.Never));
        }

        [Test]
        public void 同じ目標を設定し直しても時間は戻らない()
        {
            var state = new LockOnState();
            state.SetTarget(5L);
            state.AddTime(1f);
            state.SetTarget(5L);

            Assert.AreEqual(1f, state.Duration, 1e-6f);
        }

        [Test]
        public void 目標が変わると時間が0に戻る()
        {
            var state = new LockOnState();
            state.SetTarget(5L);
            state.AddTime(1f);
            state.SetTarget(6L);

            Assert.AreEqual(6L, state.TargetId);
            Assert.AreEqual(0f, state.Duration);
            Assert.AreEqual(LockLevel.Primary, state.GetLevel(0.5f));
        }

        [Test]
        public void nullの目標を設定するとロックが外れる()
        {
            var state = new LockOnState();
            state.SetTarget(5L);
            state.AddTime(1f);
            state.SetTarget((BaseObject)null);

            Assert.IsFalse(state.HasTarget);
            Assert.AreEqual(0f, state.Duration);
            Assert.AreEqual(LockLevel.None, state.GetLevel(0f));
        }

        [Test]
        public void 銃がなければ二次ロックにならない()
        {
            Assert.AreEqual(SecondaryLockTime.Never, SecondaryLockTime.Calc(null));
        }
    }
}
