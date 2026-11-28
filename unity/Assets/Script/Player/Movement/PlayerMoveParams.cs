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

        /// <summary>ブーストの性能（倍率・クイックブーストの速さと噴射時間・ハイパーブーストの速さ）は、装備したブースターの値で上書きする</summary>
        public static PlayerMoveParams From(PlayerControllerSettings settings, PlayerControllerConst constants, in BoostPerformance boost)
        {
            return From(settings, constants).WithBoost(boost);
        }

        /// <summary>ブーストの性能を差し替えたコピーを返す（ほかの値はそのまま）</summary>
        public PlayerMoveParams WithBoost(in BoostPerformance boost)
        {
            var p = this;
            p.HorizontalBoostRate = boost.BoostSpeedRate;
            p.QuickSpeed = boost.QuickBoostSpeed;
            p.QuickTime = boost.QuickBoostTime;
            p.HyperBoostSpeed = boost.HyperBoostSpeed;
            return p;
        }

        /// <summary>
        /// 本体のスピードを差し替えたコピーを返す。通常の移動の速さ（VecSpeed）を本体のスピードにし、
        /// クイックブーストとハイパーブーストの出力に重量補正を掛ける（ブースト ON の速さは VecSpeed × 倍率なので補正済みになる）。
        /// ブーストの性能を決めた後（WithBoost の後）に呼ぶ
        /// </summary>
        public PlayerMoveParams WithMobility(in MobilityPerformance mobility)
        {
            var p = this;
            p.VecSpeed = mobility.Speed;
            p.QuickSpeed *= mobility.WeightRate;
            p.HyperBoostSpeed *= mobility.WeightRate;
            return p;
        }

        /// <summary>ブーストの性能も PlayerControllerSettings の値（ブースターを読み込めないときの値）を使う</summary>
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
