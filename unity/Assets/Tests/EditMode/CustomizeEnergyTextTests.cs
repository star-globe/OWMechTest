using NUnit.Framework;

namespace AdvancedGears.Tests
{
    public class CustomizeEnergyTextTests
    {
        // ── 組めない理由 ──────────────────────────

        [Test]
        public void GetInvalidReasons_Valid_Empty()
        {
            var p = new EnergyPerformance(10000, 3500, 2000);

            Assert.IsEmpty(CustomizeEnergyText.GetInvalidReasons(true, p));
        }

        [Test]
        public void GetInvalidReasons_Unresolved_NoGeneratorOnly()
        {
            var reasons = CustomizeEnergyText.GetInvalidReasons(false, default);

            Assert.AreEqual(1, reasons.Count);
            Assert.AreEqual(CustomizeEnergyText.NoGeneratorReason, reasons[0]);
        }

        [Test]
        public void GetInvalidReasons_BoostOnNotPositive_OneReasonWithValue()
        {
            var p = new EnergyPerformance(10000, 1000, 0);

            var reasons = CustomizeEnergyText.GetInvalidReasons(true, p);

            Assert.AreEqual(1, reasons.Count);
            StringAssert.Contains("ブースト ON", reasons[0]);
            StringAssert.Contains("0 /秒", reasons[0]);
        }

        [Test]
        public void GetInvalidReasons_BothNotPositive_TwoReasons()
        {
            var p = new EnergyPerformance(10000, -200, -1700);

            var reasons = CustomizeEnergyText.GetInvalidReasons(true, p);

            Assert.AreEqual(2, reasons.Count);
            StringAssert.Contains("-200", reasons[0]);
            StringAssert.Contains("-1700", reasons[1]);
        }

        // ── 性能の表示 ──────────────────────────

        [Test]
        public void FormatPerformance_TotalConsumptionIsOutputMinusRecoveryOff()
        {
            var p = EnergyPerformance.Calc(10000, 5000, new[] { 300, 500, 700 }, 1500);

            var text = CustomizeEnergyText.FormatPerformance(p, 5000);

            StringAssert.Contains("総容量: 10000", text);
            StringAssert.Contains("総出力: 5000 /秒", text);
            StringAssert.Contains("常時消費の合計: 1500 /秒", text);
            StringAssert.Contains("回復力（ブースト OFF）: 3500 /秒", text);
            StringAssert.Contains("回復力（ブースト ON）: 2000 /秒", text);
        }

        // ── パーツの選択肢 ──────────────────────────

        [Test]
        public void FormatPartEnergy_NormalPart_ConstantOnly()
        {
            Assert.AreEqual("常時消費 300 /秒", CustomizeEnergyText.FormatPartEnergy(300));
        }

        [Test]
        public void FormatPartEnergy_Generator_IncludesCapacityAndOutput()
        {
            var text = CustomizeEnergyText.FormatPartEnergy(0, generator: (10000, 5000));

            StringAssert.Contains("総容量 10000", text);
            StringAssert.Contains("総出力 5000 /秒", text);
        }

        [Test]
        public void FormatPartEnergy_Booster_IncludesBoostOnConsumption()
        {
            var text = CustomizeEnergyText.FormatPartEnergy(200, boostOnConsumption: 1500);

            StringAssert.Contains("常時消費 200 /秒", text);
            StringAssert.Contains("ON の間の消費 1500 /秒", text);
        }
    }
}
