using System.Text;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面に出す重量とスピードの文言を組み立てる。Unity に依存しないのでテストできる
    /// </summary>
    public static class CustomizeMobilityText
    {
        public const string UnresolvedText = "脚部が無いため、スピードを計算できません";
        public const string NoBoosterText = "ブースター未装備（標準の性能）";

        /// <summary>重量とスピードの表示</summary>
        /// <param name="boostSpeedRate">装備しているブースターのブースト出力。ブースターが無ければ null</param>
        public static string FormatPerformance(in MobilityPerformance performance, float? boostSpeedRate)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"総重量: {performance.TotalWeight} / 積載量: {performance.LoadCapacity}");
            sb.AppendLine($"スピード（ブースト OFF）: {performance.Speed:0.0}");
            sb.Append("スピード（ブースト ON）: ");
            sb.Append(boostSpeedRate.HasValue ? $"{performance.GetBoostSpeed(boostSpeedRate.Value):0.0}" : NoBoosterText);
            return sb.ToString();
        }

        /// <summary>積載量を超えているときの警告。超えていなければ null（組めないわけではないので保存はできる）</summary>
        public static string GetOverweightWarning(in MobilityPerformance performance)
        {
            if (!performance.IsOverweight)
                return null;

            return $"積載量を超えています（{performance.TotalWeight} / {performance.LoadCapacity}）。移動の速さが ×{performance.WeightRate:0.00} になります";
        }
    }
}
