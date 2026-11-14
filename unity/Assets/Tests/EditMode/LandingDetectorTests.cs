using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class LandingDetectorTests
    {
        [Test]
        public void 地上にいるだけでは着地しない()
        {
            var detector = new LandingDetector(4f);
            Assert.IsFalse(detector.Update(false, 0f));
            Assert.IsFalse(detector.Update(false, -10f));
        }

        [Test]
        public void 速く落ちてから接地すると着地する()
        {
            var detector = new LandingDetector(4f);
            Assert.IsFalse(detector.Update(true, 8f));
            Assert.IsFalse(detector.Update(true, -8.5f));
            // 接地した瞬間は衝突で速度がほぼ 0 になっている
            Assert.IsTrue(detector.Update(false, 0f));
            Assert.AreEqual(8.5f, detector.LastFallSpeed, 1e-4f);
        }

        [Test]
        public void ゆっくり落ちてから接地しても着地しない()
        {
            var detector = new LandingDetector(4f);
            detector.Update(true, -3f);
            Assert.IsFalse(detector.Update(false, 0f));
        }

        [Test]
        public void 着地は接地した最初のフレームだけ()
        {
            var detector = new LandingDetector(4f);
            detector.Update(true, -6f);
            Assert.IsTrue(detector.Update(false, 0f));
            Assert.IsFalse(detector.Update(false, 0f));
        }

        [Test]
        public void 前の空中の落下速度は次の空中に持ち越さない()
        {
            var detector = new LandingDetector(4f);
            detector.Update(true, -6f);
            detector.Update(false, 0f);
            detector.Update(true, -1f);
            Assert.IsFalse(detector.Update(false, 0f));
        }
    }
}
