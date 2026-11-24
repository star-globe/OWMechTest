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

        [Test]
        public void 攻撃の属性が未設定なら実弾になる()
        {
            // 既存の弾のアセットには属性の項目が無いため、既定値が実弾であることに依存している
            Assert.AreEqual(AttackAttribute.Physical, default(AttackAttribute));

            var settings = UnityEngine.ScriptableObject.CreateInstance<BulletSettings>();
            try
            {
                Assert.AreEqual(AttackAttribute.Physical, settings.AttackAttribute);
                Assert.AreEqual(AttackAttribute.Physical, settings.AttackInfo.Attribute);
                Assert.AreEqual(settings.Attack, settings.AttackInfo.Power);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void 攻撃の情報は攻撃力と属性を持つ()
        {
            var attack = new AttackInfo(25, AttackAttribute.Energy);
            Assert.AreEqual(25, attack.Power);
            Assert.AreEqual(AttackAttribute.Energy, attack.Attribute);
        }
    }
}
