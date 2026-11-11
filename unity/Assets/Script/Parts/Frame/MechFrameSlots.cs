namespace AdvancedGears
{
    /// <summary>
    /// 機体本体（頭部・胴体・腕・脚）のスロット。武器と違って外せないので、未設定なら初期値のパーツを使う
    /// </summary>
    public static class MechFrameSlots
    {
        public static readonly PartsAttachType[] All = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Leg,
        };

        public static bool IsFrameSlot(PartsAttachType type)
        {
            switch (type)
            {
                case PartsAttachType.Head:
                case PartsAttachType.Core:
                case PartsAttachType.Arm:
                case PartsAttachType.Leg:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>パーツ ID が未設定（負）なら初期値の ID を返す</summary>
        public static int ResolvePartId(int partId, int defaultPartId)
        {
            return partId >= 0 ? partId : defaultPartId;
        }
    }
}
