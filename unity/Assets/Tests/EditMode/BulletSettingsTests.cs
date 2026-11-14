using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class BulletSettingsTests
    {
        [Test]
        public void マズルのエフェクトが未設定なら標準のマズルフラッシュになる()
        {
            Assert.AreEqual(EffectID.MuzzleFlash, BulletSettings.ResolveMuzzleEffectId(0));
            Assert.AreEqual(EffectID.MuzzleFlash, BulletSettings.ResolveMuzzleEffectId(-1));
        }

        [Test]
        public void マズルのエフェクトを設定していればそのIDになる()
        {
            Assert.AreEqual(EffectID.HitSpark, BulletSettings.ResolveMuzzleEffectId(EffectID.HitSpark));
        }
    }
}
