namespace AdvancedGears
{
    /// <summary>
    /// 二次ロックになるまでの時間の計算。
    /// 今は銃のマスタの固定値を返すだけ。細かい計算の仕様が決まったら、ここを差し替える
    /// </summary>
    public static class SecondaryLockTime
    {
        /// <summary>銃が装備されていないときの時間（二次ロックにならない）</summary>
        public const float Never = float.MaxValue;

        public static float Calc(GunSettings gun)
        {
            if (gun == null)
                return Never;

            return gun.SecondaryLockTime;
        }
    }
}
