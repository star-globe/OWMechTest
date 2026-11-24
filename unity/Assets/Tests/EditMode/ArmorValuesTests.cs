using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class ArmorValuesTests
    {
        [Test]
        public void 攻撃の属性に対応する装甲値を返す()
        {
            var armor = new ArmorValues(30, 70);
            Assert.AreEqual(30, armor.Get(AttackAttribute.Physical));
            Assert.AreEqual(70, armor.Get(AttackAttribute.Energy));
        }

        [Test]
        public void 属性ごとに合計する()
        {
            var total = ArmorValues.Sum(new[]
            {
                new ArmorValues(10, 1),
                new ArmorValues(20, 2),
                new ArmorValues(30, 3),
            });

            Assert.AreEqual(60, total.Physical);
            Assert.AreEqual(6, total.Energy);
        }

        [Test]
        public void パーツが無ければ合計は0()
        {
            var empty = ArmorValues.Sum(new ArmorValues[0]);
            Assert.AreEqual(0, empty.Physical);
            Assert.AreEqual(0, empty.Energy);

            var none = ArmorValues.Sum(null);
            Assert.AreEqual(0, none.Physical);
            Assert.AreEqual(0, none.Energy);
        }

        [Test]
        public void 装甲値が未設定のパーツとユニットは0で装甲を持つ()
        {
            // 既存のパーツ・ユニットのアセットには装甲の項目が無いため、既定値に依存している
            var part = UnityEngine.ScriptableObject.CreateInstance<PartSettings>();
            var unit = UnityEngine.ScriptableObject.CreateInstance<TurretParameterSettings>();
            try
            {
                Assert.AreEqual(0, part.Armor.Physical);
                Assert.AreEqual(0, part.Armor.Energy);

                Assert.IsTrue(unit.HasArmor);
                Assert.AreEqual(0, unit.Armor.Physical);
                Assert.AreEqual(0, unit.Armor.Energy);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(part);
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }

        [Test]
        public void 装甲を持たないユニットは装甲なしになる()
        {
            var unit = UnityEngine.ScriptableObject.CreateInstance<TurretParameterSettings>();
            try
            {
                var withArmor = new UnitParam();
                withArmor.SetInitialInfo(unit);
                Assert.IsTrue(withArmor.Armor.HasValue, "装甲を持つユニットは装甲値 0 でも装甲あり");

                var serialized = new UnityEditor.SerializedObject(unit);
                serialized.FindProperty("hasArmor").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var withoutArmor = new UnitParam();
                withoutArmor.SetInitialInfo(unit);
                Assert.IsFalse(withoutArmor.Armor.HasValue);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }
    }
}
