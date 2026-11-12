using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Parts/BoosterSettings", order = 0)]
    public class BoosterSettings : PartSettings
    {
        [SerializeField]
        int boostPower;
        public int BoostPower => boostPower;

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
