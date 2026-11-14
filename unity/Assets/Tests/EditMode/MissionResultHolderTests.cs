using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class MissionResultHolderTests
    {
        [Test]
        public void 初期状態は未決定()
        {
            var holder = new MissionResultHolder();
            Assert.AreEqual(MissionResult.None, holder.Result);
            Assert.IsFalse(holder.IsDecided);
        }

        [Test]
        public void 最初に決まった結果だけを残す()
        {
            var holder = new MissionResultHolder();
            Assert.IsTrue(holder.TryDecide(MissionResult.Failed));
            Assert.IsFalse(holder.TryDecide(MissionResult.Success));
            Assert.AreEqual(MissionResult.Failed, holder.Result);
        }

        [Test]
        public void Noneでは決まらない()
        {
            var holder = new MissionResultHolder();
            Assert.IsFalse(holder.TryDecide(MissionResult.None));
            Assert.IsFalse(holder.IsDecided);
        }

        [Test]
        public void リセットすると次の戦闘で決め直せる()
        {
            var holder = new MissionResultHolder();
            holder.TryDecide(MissionResult.Success);
            holder.Reset();
            Assert.IsFalse(holder.IsDecided);
            Assert.IsTrue(holder.TryDecide(MissionResult.Failed));
            Assert.AreEqual(MissionResult.Failed, holder.Result);
        }
    }
}
