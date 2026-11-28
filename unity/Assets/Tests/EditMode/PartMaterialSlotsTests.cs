using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class PartMaterialSlotsTests
    {
        [TestCase("Mat_Main", PartMaterialKind.Main)]
        [TestCase("Mat_Sub", PartMaterialKind.Sub)]
        [TestCase("Mat_Light", PartMaterialKind.Light)]
        [TestCase("Mat_Thruster", PartMaterialKind.Thruster)]
        public void スロット名から種類を見分ける(string name, PartMaterialKind expected)
        {
            Assert.AreEqual(expected, PartMaterialSlots.GetKind(name));
        }

        [Test]
        public void インスタンス化で付く接尾辞は無視する()
        {
            Assert.AreEqual(PartMaterialKind.Main, PartMaterialSlots.GetKind("Mat_Main (Instance)"));
            Assert.AreEqual(PartMaterialKind.Light, PartMaterialSlots.GetKind("Mat_Light (Instance)"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Mat_Body")]
        [TestCase("Mat_BoosterFlame")]
        [TestCase("Default-Material")]
        public void 規約に無い名前は_None(string name)
        {
            Assert.AreEqual(PartMaterialKind.None, PartMaterialSlots.GetKind(name));
        }
    }
}
