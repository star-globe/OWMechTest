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
