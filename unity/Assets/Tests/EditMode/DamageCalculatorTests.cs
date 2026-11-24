using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class DamageCalculatorTests
    {
        const int Reference = 1000;

        static AttackInfo Physical(int power) => new AttackInfo(power, AttackAttribute.Physical);
        static AttackInfo Energy(int power) => new AttackInfo(power, AttackAttribute.Energy);

        [Test]
        public void 装甲を持たない対象には攻撃力がそのままダメージになる()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(Physical(100), null, Reference));
            Assert.AreEqual(100, DamageCalculator.Calculate(Energy(100), null, Reference));
        }

        [Test]
        public void 装甲値0なら攻撃力がそのままダメージになる()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(Physical(100), ArmorValues.Zero, Reference));
        }

        [Test]
        public void 装甲値が基準値と同じならダメージは半分()
        {
            var armor = new ArmorValues(Reference, Reference);
            Assert.AreEqual(50, DamageCalculator.Calculate(Physical(100), armor, Reference));
            Assert.AreEqual(50, DamageCalculator.Calculate(Energy(100), armor, Reference));
        }

        [Test]
        public void 攻撃と同じ属性の装甲値で減る()
        {
            // 実弾装甲だけが高い
            var armor = new ArmorValues(Reference, 0);
            Assert.AreEqual(50, DamageCalculator.Calculate(Physical(100), armor, Reference));
            Assert.AreEqual(100, DamageCalculator.Calculate(Energy(100), armor, Reference));
        }

        [Test]
        public void 小数は四捨五入する()
        {
            // 100 × 1000 / 1450 = 68.96… → 69
            Assert.AreEqual(69, DamageCalculator.Calculate(Physical(100), new ArmorValues(450, 450), Reference));
            // 5 × 1000 / (1000 + 1000) = 2.5 → 3（.5 は切り上げ）
            Assert.AreEqual(3, DamageCalculator.Calculate(Physical(5), new ArmorValues(Reference, 0), Reference));
        }

        [Test]
        public void 攻撃力が1以上なら最低1()
        {
            var armor = new ArmorValues(1000000, 1000000);
            Assert.AreEqual(1, DamageCalculator.Calculate(Physical(1), armor, Reference));
            Assert.AreEqual(1, DamageCalculator.Calculate(Energy(10), armor, Reference));
        }

        [Test]
        public void 攻撃力が0以下ならダメージは0()
        {
            Assert.AreEqual(0, DamageCalculator.Calculate(Physical(0), ArmorValues.Zero, Reference));
            Assert.AreEqual(0, DamageCalculator.Calculate(Physical(0), null, Reference));
            Assert.AreEqual(0, DamageCalculator.Calculate(Physical(-5), new ArmorValues(10, 10), Reference));
        }

        [Test]
        public void 基準値が不正なら装甲は効かない()
        {
            var armor = new ArmorValues(500, 500);
            Assert.AreEqual(100, DamageCalculator.Calculate(Physical(100), armor, 0));
            Assert.AreEqual(100, DamageCalculator.Calculate(Physical(100), armor, -1));
        }
    }
}
