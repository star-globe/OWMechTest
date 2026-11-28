using System;

namespace AdvancedGears
{
    /// <summary>重量補正の計算に使う、全機体で共通の定数（PlayerControllerConstSettings で設定する）</summary>
    public readonly struct MobilityConst
    {
        public const float DefaultOverweightSlowRate = 2.0f;
        public const float DefaultMinWeightRate = 0.2f;

        /// <summary>積載量の超過による減速の強さ。重量補正 = 1 − これ × (積載率 − 1)</summary>
        public readonly float OverweightSlowRate;

        /// <summary>重量補正の下限</summary>
        public readonly float MinWeightRate;

        public MobilityConst(float overweightSlowRate, float minWeightRate)
        {
            OverweightSlowRate = overweightSlowRate;
            MinWeightRate = minWeightRate;
        }

        public static MobilityConst Default => new MobilityConst(DefaultOverweightSlowRate, DefaultMinWeightRate);
    }

    /// <summary>
    /// 本体のスピードの性能。脚部の歩行速度と積載量、脚部以外の総重量で決まる。
    /// 本体のスピード（ブースト OFF）= 歩行速度 × 重量補正。重量補正は積載量以内なら 1 で、
    /// 超えると超過分に応じて下がる（下限あり）。クイックブーストとハイパーブーストの出力にも重量補正を掛ける
    /// </summary>
    public readonly struct MobilityPerformance
    {
        /// <summary>脚部以外の総重量</summary>
        public readonly int TotalWeight;

        /// <summary>脚部の積載量</summary>
        public readonly int LoadCapacity;

        /// <summary>脚部の歩行速度</summary>
        public readonly float WalkSpeed;

        /// <summary>重量補正（移動の速さに掛ける倍率）</summary>
        public readonly float WeightRate;

        public MobilityPerformance(int totalWeight, int loadCapacity, float walkSpeed, in MobilityConst constants)
        {
            TotalWeight = totalWeight;
            LoadCapacity = loadCapacity;
            WalkSpeed = walkSpeed;
            WeightRate = CalcWeightRate(totalWeight, loadCapacity, constants);
        }

        /// <summary>積載量を超えているか（ちょうど積載量なら超えていない）</summary>
        public bool IsOverweight => TotalWeight > LoadCapacity;

        /// <summary>本体のスピード（ブースト OFF の移動の速さ）</summary>
        public float Speed => WalkSpeed * WeightRate;

        /// <summary>ブースト ON の移動の速さ = 本体のスピード × ブースト出力</summary>
        public float GetBoostSpeed(float boostSpeedRate) => Speed * boostSpeedRate;

        /// <summary>
        /// 重量補正。積載率（総重量 ÷ 積載量）が 1 以下なら 1。超えたら 1 − 超過減速率 × (積載率 − 1) で、下限で止める。
        /// 積載量が 0 以下のときは、重量があれば下限、無ければ 1 にする
        /// </summary>
        public static float CalcWeightRate(int totalWeight, int loadCapacity, in MobilityConst constants)
        {
            var min = Math.Max(0f, Math.Min(1f, constants.MinWeightRate));
            if (totalWeight <= loadCapacity)
                return 1f;
            if (loadCapacity <= 0)
                return min;

            var loadRate = (float) totalWeight / loadCapacity;
            var rate = 1f - constants.OverweightSlowRate * (loadRate - 1f);
            return Math.Max(min, Math.Min(1f, rate));
        }
    }

    /// <summary>装備の組み合わせからマスタを引いて、本体のスピードの性能を決める</summary>
    public static class MobilityPerformanceResolver
    {
        /// <summary>重量を数えるスロット（脚部以外のすべて。左右の武器は別々に数える）</summary>
        public static readonly PartsAttachType[] WeightSlots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Booster,
            PartsAttachType.Generator,
            PartsAttachType.Weapon_Right,
            PartsAttachType.Weapon_Left,
            PartsAttachType.Weapon_Sub,
        };

        /// <summary>
        /// 装備から性能を計算する。脚部が未装備・マスタに無い・歩行速度が 0 のときは false（機体の設定の速さを使う）。
        /// マスタに無いパーツ ID と未装備（負の ID）は重量に数えない（エラーログを出さないよう TryGetValue を使う）
        /// </summary>
        /// <param name="getPartId">スロットに装備しているパーツ ID（未装備は負）</param>
        public static bool TryResolve(Func<PartsAttachType, int> getPartId, in MobilityConst constants, out MobilityPerformance performance)
        {
            var parts = PartMaster.Instance.GetDictionary();

            var legId = getPartId(PartsAttachType.Leg);
            // 歩行速度が未設定（0）の脚部では動けなくなるので、読み込めないときと同じ扱いにする
            if (legId < 0 || !parts.TryGetValue(legId, out var leg) || leg == null || leg.WalkSpeed <= 0f)
            {
                performance = default;
                return false;
            }

            var totalWeight = 0;
            foreach (var slot in WeightSlots)
            {
                var partId = getPartId(slot);
                if (partId >= 0 && parts.TryGetValue(partId, out var part) && part != null)
                    totalWeight += part.Weight;
            }

            performance = new MobilityPerformance(totalWeight, leg.LoadCapacity, leg.WalkSpeed, constants);
            return true;
        }
    }
}
