using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Player/PlayerControllerConstSettings", order = 0)]
    public class PlayerControllerConstSettings : ScriptableObject
    {
        [SerializeField]
        float rotXMin = 0.2f;
        public float RotXMin => rotXMin;

        [SerializeField]
        float rotYMin = 0.2f;
        public float RotYMin => rotYMin;

        [SerializeField]
        float rotYLim = 80.0f;
        public float RotYLim => rotYLim;

        [SerializeField]
        float wallJumpRate = 0.5f;
        public float WallJumpRate => wallJumpRate;

        [SerializeField]
        float jumpTime = 0.5f;
        public float JumpTime => jumpTime;

        [SerializeField]
        float touchTimeRate = 0.2f;
        public float TouchTime => jumpTime * touchTimeRate;

        [SerializeField]
        float jumpInterval = 0.2f;
        public float JumpInterval => jumpInterval;

        [SerializeField]
        float airBrakeRate = 0.2f;
        public float AirBrakeRate => airBrakeRate;

        [SerializeField]
        float touchBrakeRateAdd = 0.2f;
        public float TouchBrakeRateAdd => touchBrakeRateAdd;

        [SerializeField]
        float quickYdelta = 0.1f;
        public float QuickYDelta => quickYdelta;

        [SerializeField, Min(0f)]
        [Tooltip("積載量の超過による減速の強さ。重量補正 = 1 − これ × (積載率 − 1)。積載量以内なら減速しない")]
        float overweightSlowRate = MobilityConst.DefaultOverweightSlowRate;
        public float OverweightSlowRate => overweightSlowRate;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("重量補正の下限（積載量を大きく超えたときの最低の倍率）")]
        float minWeightRate = MobilityConst.DefaultMinWeightRate;
        public float MinWeightRate => minWeightRate;
    }

    public class PlayerControllerConst : Singleton<PlayerControllerConst>
    {
        const string resourcesPath = "PlayerControllerConstSettings";

        PlayerControllerConstSettings settings = null;

        private PlayerControllerConstSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    throw new Exception("PlayerControllerConstSettings is Null!");
                }

                return settings;
            }
        }

        public float RotXMin => this.Settings.RotXMin;
        public float RotYMin => this.Settings.RotYMin;
        public float RotYLim => this.Settings.RotYLim;
        public float WallJumpRate => this.Settings.WallJumpRate;
        public float JumpTime => this.Settings.JumpTime;
        public float JumpInterval => this.Settings.JumpInterval;
        public float TouchTime => this.Settings.TouchTime;
        public float AirBrakeRate => this.Settings.AirBrakeRate;
        public float TouchBrakeRateAdd => this.Settings.TouchBrakeRateAdd;
        public float QuickYDelta => this.Settings.QuickYDelta;

        /// <summary>重量補正の計算に使う定数</summary>
        public MobilityConst Mobility => new MobilityConst(this.Settings.OverweightSlowRate, this.Settings.MinWeightRate);

        public void Load()
        {
            settings = Resources.Load<PlayerControllerConstSettings>(resourcesPath);
        }
    }
}
