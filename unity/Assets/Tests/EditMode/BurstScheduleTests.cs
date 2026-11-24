using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class BurstScheduleTests
    {
        [Test]
        public void 最初の1発はすぐに撃ち残りは間隔ごとに撃つ()
        {
            var burst = new BurstSchedule();
            // 間隔は 2 進数で正確に表せる 0.25 秒にする（境界ちょうどの比較で浮動小数の誤差が出ないように）
            burst.Start(4, 0.25f, 10.0);

            Assert.AreEqual(1, burst.ConsumeDue(10.0));
            Assert.AreEqual(0, burst.ConsumeDue(10.1));
            Assert.AreEqual(1, burst.ConsumeDue(10.25));
            Assert.AreEqual(1, burst.ConsumeDue(10.5));
            Assert.IsTrue(burst.IsActive);
            Assert.AreEqual(1, burst.ConsumeDue(10.75));
            Assert.IsFalse(burst.IsActive);
            Assert.AreEqual(0, burst.ConsumeDue(11.0));
        }

        [Test]
        public void フレームが遅れたら遅れた分をまとめて撃つ()
        {
            var burst = new BurstSchedule();
            burst.Start(4, 0.2f, 0.0);

            Assert.AreEqual(3, burst.ConsumeDue(0.45));
            Assert.AreEqual(1, burst.Remaining);
        }

        [Test]
        public void 数が1なら1発だけ撃つ()
        {
            var burst = new BurstSchedule();
            burst.Start(1, 0f, 0.0);

            Assert.AreEqual(1, burst.ConsumeDue(0.0));
            Assert.IsFalse(burst.IsActive);
        }

        [Test]
        public void 数が0以下でも1発は撃つ()
        {
            var burst = new BurstSchedule();
            burst.Start(0, 0f, 0.0);

            Assert.AreEqual(1, burst.ConsumeDue(0.0));
        }

        [Test]
        public void 取り消すと残りは撃たない()
        {
            var burst = new BurstSchedule();
            burst.Start(4, 0.2f, 0.0);
            burst.ConsumeDue(0.0);
            burst.Cancel();

            Assert.IsFalse(burst.IsActive);
            Assert.AreEqual(0, burst.ConsumeDue(1.0));
        }
    }
}
