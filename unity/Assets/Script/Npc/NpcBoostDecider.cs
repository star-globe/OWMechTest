namespace AdvancedGears
{
    /// <summary>
    /// NPC のブーストの切り替え（暫定のロジック）。
    /// ON のときは残量が 10% 以下になったら OFF、OFF のときは 50% 以上になったら ON にする
    /// </summary>
    public static class NpcBoostDecider
    {
        public const float TurnOffRate = 0.1f;
        public const float TurnOnRate = 0.5f;

        /// <summary>ブーストの ON / OFF を切り替えるか</summary>
        public static bool ShouldToggle(bool isBoost, int energy, int maxEnergy)
        {
            if (maxEnergy <= 0)
                return isBoost;     // 性能が無いときは OFF のままにする

            var rate = (float)energy / maxEnergy;
            return isBoost ? rate <= TurnOffRate : rate >= TurnOnRate;
        }
    }
}
