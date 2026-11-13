using System.Collections.Generic;
using System.Text;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面に出すエネルギーの文言（性能、組めない理由、パーツの選択肢の補足）を組み立てる。
    /// Unity に依存しないのでテストできる
    /// </summary>
    public static class CustomizeEnergyText
    {
        public const string NoGeneratorReason = "ジェネレーターが装備されていません";
        public const string SavedDataInvalidWarning = "保存済みの組み合わせは、マスタの調整で組めなくなっています。出撃するときは初期の組み合わせを使います";

        /// <summary>
        /// 性能の表示。常時消費の合計は「総出力 − ブースト OFF の回復力」で求める
        /// </summary>
        /// <param name="output">ジェネレーターの総出力（1 秒あたり）</param>
        public static string FormatPerformance(EnergyPerformance performance, int output)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"総容量: {performance.MaxEnergy}");
            sb.AppendLine($"総出力: {output} /秒");
            sb.AppendLine($"常時消費の合計: {output - performance.RecoveryBoostOff} /秒");
            sb.AppendLine($"回復力（ブースト OFF）: {performance.RecoveryBoostOff} /秒");
            sb.Append($"回復力（ブースト ON）: {performance.RecoveryBoostOn} /秒");
            return sb.ToString();
        }

        /// <summary>ジェネレーターを読み込めず、性能を計算できないときの表示</summary>
        public static string FormatUnresolvedPerformance()
        {
            return "ジェネレーターが無いため、性能を計算できません";
        }

        /// <summary>組めない理由。組めるなら空のリスト</summary>
        /// <param name="resolved">性能を計算できたか（EnergyPerformanceResolver.TryResolve の戻り値）</param>
        public static List<string> GetInvalidReasons(bool resolved, EnergyPerformance performance)
        {
            var reasons = new List<string>();
            if (!resolved)
            {
                reasons.Add(NoGeneratorReason);
                return reasons;
            }

            if (performance.RecoveryBoostOff <= 0)
                reasons.Add($"ブースト OFF の回復力が 0 以下です（{performance.RecoveryBoostOff} /秒）");
            if (performance.RecoveryBoostOn <= 0)
                reasons.Add($"ブースト ON の回復力が 0 以下です（{performance.RecoveryBoostOn} /秒）");
            return reasons;
        }

        /// <summary>パーツの選択肢に出す、エネルギーの値</summary>
        /// <param name="constantConsumption">常時消費（1 秒あたり）</param>
        /// <param name="generator">ジェネレーターなら総容量と総出力</param>
        /// <param name="boostOnConsumption">ブースターなら ON の間の消費（1 秒あたり）</param>
        public static string FormatPartEnergy(int constantConsumption, (int capacity, int output)? generator = null, int? boostOnConsumption = null)
        {
            var sb = new StringBuilder($"常時消費 {constantConsumption} /秒");
            if (generator.HasValue)
                sb.Append($"　総容量 {generator.Value.capacity}　総出力 {generator.Value.output} /秒");
            if (boostOnConsumption.HasValue)
                sb.Append($"　ON の間の消費 {boostOnConsumption.Value} /秒");
            return sb.ToString();
        }
    }
}
