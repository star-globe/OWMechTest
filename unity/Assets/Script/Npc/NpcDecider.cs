namespace AdvancedGears
{
    /// <summary>
    /// NPC の行動の判断に使う、その時点の状態。NpcController が毎フレーム作って INpcDecider に渡す
    /// </summary>
    public readonly struct NpcDecisionContext
    {
        public readonly bool IsBoost;
        public readonly int Energy;
        public readonly int MaxEnergy;
        public readonly bool IsFloating;
        public readonly bool HasLockTarget;
        /// <summary>経路の目的地が自分よりどれだけ高いか（m）。経路が無ければ null</summary>
        public readonly float? HeightToDestination;

        public NpcDecisionContext(bool isBoost, int energy, int maxEnergy, bool isFloating,
                                  bool hasLockTarget, float? heightToDestination)
        {
            IsBoost = isBoost;
            Energy = energy;
            MaxEnergy = maxEnergy;
            IsFloating = isFloating;
            HasLockTarget = hasLockTarget;
            HeightToDestination = heightToDestination;
        }
    }

    /// <summary>
    /// NPC の行動（ブースト・ジャンプ・クイックブースト・ハイパーブースト・上昇・射撃）を判断する。
    /// AI を作り込むときは、この実装を差し替える（NpcController.Decider）
    /// </summary>
    public interface INpcDecider
    {
        /// <summary>ブーストの ON / OFF を切り替えるか</summary>
        bool ShouldToggleBoost(in NpcDecisionContext context);
        /// <summary>ジャンプするか（接地やジャンプの間隔は PlayerController 側で判定する）</summary>
        bool ShouldJump(in NpcDecisionContext context);
        /// <summary>クイックブーストを出すか</summary>
        bool ShouldQuick(in NpcDecisionContext context);
        /// <summary>ハイパーブーストを始めるか（ブースト中かどうかは PlayerController 側で判定する）</summary>
        bool ShouldHyperBoost(in NpcDecisionContext context);
        /// <summary>グライド中の上昇をするか（空中・ブースト中・エネルギーは PlayerController 側で判定する）</summary>
        bool ShouldAscend(in NpcDecisionContext context);
        /// <summary>その武器で射撃するか</summary>
        bool ShouldFire(WeaponSlot slot, in NpcDecisionContext context);
    }

    /// <summary>
    /// NPC の行動の判断（暫定のロジック）。
    /// - ブースト: ON のときは残量が 10% 以下になったら OFF、OFF のときは 50% 以上になったら ON にする
    /// - ジャンプ・クイックブースト・ハイパーブースト: 当面は使わない
    /// - 上昇: 経路の目的地が ascendHeight 以上高いときだけ
    /// - 射撃: ロックしている間
    /// </summary>
    public class NpcDecider : INpcDecider
    {
        public const float TurnOffRate = 0.1f;
        public const float TurnOnRate = 0.5f;

        readonly float ascendHeight;

        public NpcDecider(float ascendHeight)
        {
            this.ascendHeight = ascendHeight;
        }

        public bool ShouldToggleBoost(in NpcDecisionContext context)
        {
            return ShouldToggleBoost(context.IsBoost, context.Energy, context.MaxEnergy);
        }

        /// <summary>ブーストの ON / OFF を切り替えるか</summary>
        public static bool ShouldToggleBoost(bool isBoost, int energy, int maxEnergy)
        {
            if (maxEnergy <= 0)
                return isBoost;     // 性能が無いときは OFF のままにする

            var rate = (float)energy / maxEnergy;
            return isBoost ? rate <= TurnOffRate : rate >= TurnOnRate;
        }

        public bool ShouldJump(in NpcDecisionContext context) => false;

        public bool ShouldQuick(in NpcDecisionContext context) => false;

        public bool ShouldHyperBoost(in NpcDecisionContext context) => false;

        public bool ShouldAscend(in NpcDecisionContext context)
        {
            return context.HeightToDestination.HasValue && context.HeightToDestination.Value >= ascendHeight;
        }

        public bool ShouldFire(WeaponSlot slot, in NpcDecisionContext context)
        {
            return context.HasLockTarget;
        }
    }
}
