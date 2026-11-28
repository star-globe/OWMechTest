using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Parts/BoosterSettings", order = 0)]
    public class BoosterSettings : PartSettings
    {
        /// <summary>ブースト出力。ブースト ON の間、本体のスピード（MobilityPerformance）に掛ける倍率</summary>
        [SerializeField, Min(0f)]
        float boostSpeedRate = 2.0f;
        public float BoostSpeedRate => boostSpeedRate;

        /// <summary>クイックブーストの出力（移動の速さ）</summary>
        [SerializeField, Min(0f)]
        float quickBoostSpeed = 15.0f;
        public float QuickBoostSpeed => quickBoostSpeed;

        /// <summary>クイックブーストの噴射時間（秒）</summary>
        [SerializeField, Min(0f)]
        float quickBoostTime = 0.1f;
        public float QuickBoostTime => quickBoostTime;

        /// <summary>クイックブーストの消費エネルギー（1 回あたり）</summary>
        [SerializeField, Min(0)]
        int quickBoostEnergy = 2500;
        public int QuickBoostEnergy => quickBoostEnergy;

        /// <summary>ハイパーブーストの出力（移動の速さ）</summary>
        [SerializeField, Min(0f)]
        float hyperBoostSpeed = 30.0f;
        public float HyperBoostSpeed => hyperBoostSpeed;

        /// <summary>ハイパーブーストの消費エネルギー（1 秒あたり）</summary>
        [SerializeField, Min(0)]
        int hyperBoostEnergy = 4000;
        public int HyperBoostEnergy => hyperBoostEnergy;

        /// <summary>ブーストが ON の間、常時消費に加えて消費するエネルギー（1 秒あたり。地上でも空中でも）</summary>
        [SerializeField, Min(0)]
        int boostOnEnergyConsumption = 1500;
        public int BoostOnEnergyConsumption => boostOnEnergyConsumption;

        /// <summary>エフェクトとノズルの発光の基本色（HDR）。各状態の色はここから計算する（BoosterColorPalette）</summary>
        [SerializeField, ColorUsage(false, true)]
        Color baseColor = BoosterColorPalette.DefaultBaseColor;
        public Color BaseColor => baseColor;
    }

    public class BoosterMasterContainer : IDBasedMasterContainer<BoosterSettings>
    {
        protected override string resourcesFolder => "BoosterSettings";
    }

    public class BoosterMaster : Singleton<BoosterMasterContainer>
    {
        public static BoosterPart InstantiateBoosterPart(int boosterId)
        {
            // 負の ID は未装備（RemoveBoosters も -1 を渡す）
            if (boosterId < 0)
                return null;

            return Instance.GetSettings(boosterId)?.InstantiatePart<BoosterPart>();
        }
    }
}
