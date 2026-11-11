namespace AdvancedGears
{
    /// <summary>
    /// 移動の計算に使うパラメーター。PlayerControllerSettings（機体ごと）と PlayerControllerConst（共通）の値をまとめる。
    /// Editor で設定を変えたときにすぐ反映されるよう、PlayerController は毎フレーム From で作り直す。
    /// </summary>
    public struct PlayerMoveParams
    {
        // PlayerControllerSettings
        public float VecSpeed;
        public float AccelerateTime;
        public float JumpSpeed;
        public float HorizontalBoostRate;
        public float FloatRate;
        public float RotSpeed;
        public float QuickSpeed;
        public float TouchQuickRate;
        public float QuickTime;
        public float QuickInterval;
        public float HyperBoostSpeed;
        public float AscendSpeed;
        public float AscendAccel;

        // PlayerControllerConst
        public float RotXMin;
        public float WallJumpRate;
        public float JumpTime;
        public float TouchTime;
        public float JumpInterval;
        public float AirBrakeRate;
        public float TouchBrakeRateAdd;
        public float QuickYDelta;

        public static PlayerMoveParams From(PlayerControllerSettings settings, PlayerControllerConst constants)
        {
            return new PlayerMoveParams
            {
                VecSpeed = settings.VecSpeed,
                AccelerateTime = settings.AccelerateTime,
                JumpSpeed = settings.JumpSpeed,
                HorizontalBoostRate = settings.HorizontalBoostRate,
                FloatRate = settings.FloatRate,
                RotSpeed = settings.RotSpeed,
                QuickSpeed = settings.QuickSpeed,
                TouchQuickRate = settings.TouchQuickRate,
                QuickTime = settings.QuickTime,
                QuickInterval = settings.QuickInterval,
                HyperBoostSpeed = settings.HyperBoostSpeed,
                AscendSpeed = settings.AscendSpeed,
                AscendAccel = settings.AscendAccel,

                RotXMin = constants.RotXMin,
                WallJumpRate = constants.WallJumpRate,
                JumpTime = constants.JumpTime,
                TouchTime = constants.TouchTime,
                JumpInterval = constants.JumpInterval,
                AirBrakeRate = constants.AirBrakeRate,
                TouchBrakeRateAdd = constants.TouchBrakeRateAdd,
                QuickYDelta = constants.QuickYDelta,
            };
        }
    }
}
