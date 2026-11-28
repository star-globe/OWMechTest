using System;

namespace AdvancedGears
{
    /// <summary>
    /// ブースト・クイックブースト・ハイパーブーストの性能。装備したブースターで決まる。
    /// ブースターを装備していない・読み込めないときは、機体の設定の値（読み込めないときの値）を使う
    /// </summary>
    public readonly struct BoostPerformance
    {
        /// <summary>ブースト出力。ブースト ON の間、通常の水平の移動スピードに掛ける倍率</summary>
        public readonly float BoostSpeedRate;

        /// <summary>クイックブーストの出力（移動の速さ）</summary>
        public readonly float QuickBoostSpeed;

        /// <summary>クイックブーストの噴射時間（秒）</summary>
        public readonly float QuickBoostTime;

        /// <summary>クイックブーストの消費エネルギー（1 回あたり）</summary>
        public readonly int QuickBoostEnergy;

        /// <summary>ハイパーブーストの出力（移動の速さ）</summary>
        public readonly float HyperBoostSpeed;

        /// <summary>ハイパーブーストの消費エネルギー（1 秒あたり）</summary>
        public readonly int HyperBoostEnergy;

        public BoostPerformance(float boostSpeedRate, float quickBoostSpeed, float quickBoostTime, int quickBoostEnergy,
                                float hyperBoostSpeed, int hyperBoostEnergy)
        {
            BoostSpeedRate = boostSpeedRate;
            QuickBoostSpeed = quickBoostSpeed;
            QuickBoostTime = quickBoostTime;
            QuickBoostEnergy = quickBoostEnergy;
            HyperBoostSpeed = hyperBoostSpeed;
            HyperBoostEnergy = hyperBoostEnergy;
        }

        /// <summary>ブースターのマスタの値から作る</summary>
        public static BoostPerformance FromBooster(BoosterSettings booster)
        {
            return new BoostPerformance(booster.BoostSpeedRate, booster.QuickBoostSpeed, booster.QuickBoostTime,
                                        booster.QuickBoostEnergy, booster.HyperBoostSpeed, booster.HyperBoostEnergy);
        }

        /// <summary>ブースターがあればその値、無ければ（未装備・読み込めない）fallback を返す</summary>
        public static BoostPerformance Resolve(BoosterSettings booster, in BoostPerformance fallback)
        {
            return booster != null ? FromBooster(booster) : fallback;
        }
    }

    /// <summary>装備の組み合わせからブースターのマスタを引いて、ブーストの性能を決める</summary>
    public static class BoostPerformanceResolver
    {
        /// <summary>
        /// 装備しているブースターの性能を返す。未装備（負の ID）やマスタに無い ID なら fallback を返す。
        /// マスタに無い ID はエラーログを出さずに扱う（GetSettings ではなく TryGetValue を使う）
        /// </summary>
        /// <param name="getPartId">スロットに装備しているパーツ ID（未装備は負）</param>
        public static BoostPerformance Resolve(Func<PartsAttachType, int> getPartId, in BoostPerformance fallback)
        {
            BoosterSettings booster = null;
            var boosterId = getPartId(PartsAttachType.Booster);
            if (boosterId >= 0)
                BoosterMaster.Instance.GetDictionary().TryGetValue(boosterId, out booster);

            return BoostPerformance.Resolve(booster, fallback);
        }
    }
}
