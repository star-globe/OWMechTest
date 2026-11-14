using System.Collections.Generic;

namespace AdvancedGears
{
    /// <summary>
    /// 機体のエネルギーの性能（最大エネルギーと、ブースト OFF / ON それぞれの回復力、オーバーヒートの時間と復帰エネルギー）。
    /// 回復力 ＝ ジェネレーターの総出力 − 装備した各パーツの常時消費の合計（ブースト ON の間はブースターの ON 消費も引く）。
    /// Unity に依存しないのでテストできる
    /// </summary>
    public readonly struct EnergyPerformance
    {
        /// <summary>オーバーヒート時間の初期値（秒）。ジェネレーターも機体の設定も無いとき（テストなど）に使う</summary>
        public const float DefaultOverheatTime = 3f;

        /// <summary>復帰エネルギーの初期値（最大エネルギーに対する割合）</summary>
        public const float DefaultOverheatRecoveryRate = 0.3f;

        public readonly int MaxEnergy;
        public readonly int RecoveryBoostOff;
        public readonly int RecoveryBoostOn;

        /// <summary>オーバーヒートしてから解除されるまでの時間（秒）</summary>
        public readonly float OverheatTime;

        /// <summary>復帰エネルギーの、最大エネルギーに対する割合（0〜1）</summary>
        public readonly float OverheatRecoveryRate;

        public EnergyPerformance(int maxEnergy, int recoveryBoostOff, int recoveryBoostOn,
                                 float overheatTime = DefaultOverheatTime,
                                 float overheatRecoveryRate = DefaultOverheatRecoveryRate)
        {
            MaxEnergy = maxEnergy;
            RecoveryBoostOff = recoveryBoostOff;
            RecoveryBoostOn = recoveryBoostOn;
            OverheatTime = overheatTime < 0f ? 0f : overheatTime;
            OverheatRecoveryRate = overheatRecoveryRate < 0f ? 0f : (overheatRecoveryRate > 1f ? 1f : overheatRecoveryRate);
        }

        /// <summary>組める組み合わせか。ブースト OFF / ON のどちらでも回復力が 0 より大きい</summary>
        public bool IsValid => RecoveryBoostOff > 0 && RecoveryBoostOn > 0;

        /// <summary>復帰エネルギー。オーバーヒートが解除されたときのエネルギーの残量</summary>
        public int OverheatRecoveryEnergy => (int) (MaxEnergy * OverheatRecoveryRate);

        /// <summary>今の状態での回復力（1 秒あたり）</summary>
        public int GetRecovery(bool isBoost)
        {
            return isBoost ? RecoveryBoostOn : RecoveryBoostOff;
        }

        /// <param name="capacity">ジェネレーターの総容量</param>
        /// <param name="output">ジェネレーターの総出力（1 秒あたり）</param>
        /// <param name="constantConsumptions">装備したパーツの常時消費。左右に同じ武器を持つなら 2 回入れる</param>
        /// <param name="boostOnConsumption">ブースト ON の間に加わる消費（ブースターの ON 消費）</param>
        /// <param name="overheatTime">ジェネレーターのオーバーヒート時間（秒）</param>
        /// <param name="overheatRecoveryRate">ジェネレーターの復帰エネルギー（最大エネルギーに対する割合）</param>
        public static EnergyPerformance Calc(int capacity, int output, IEnumerable<int> constantConsumptions, int boostOnConsumption,
                                             float overheatTime = DefaultOverheatTime,
                                             float overheatRecoveryRate = DefaultOverheatRecoveryRate)
        {
            var total = 0;
            if (constantConsumptions != null)
            {
                foreach (var c in constantConsumptions)
                    total += c;
            }

            var off = output - total;
            return new EnergyPerformance(capacity, off, off - boostOnConsumption, overheatTime, overheatRecoveryRate);
        }

        /// <summary>
        /// ジェネレーターを読み込めないときの性能。パーツを見ずに、機体の設定（PlayerParameterSettings）の値を使う
        /// （ON の間は以前の「浮遊中のブースト消費」と同じ量だけ回復力を下げる）
        /// </summary>
        public static EnergyPerformance FromFixedValues(int maxEnergy, int recovery, int boostOnConsumption,
                                                        float overheatTime = DefaultOverheatTime,
                                                        float overheatRecoveryRate = DefaultOverheatRecoveryRate)
        {
            return new EnergyPerformance(maxEnergy, recovery, recovery - boostOnConsumption, overheatTime, overheatRecoveryRate);
        }
    }
}
