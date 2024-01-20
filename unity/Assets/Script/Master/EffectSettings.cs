using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Effect/EffectSettings", order = 0)]
    public class EffectSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField]
        int effectId;
        public int EffectID => effectId;

        [SerializeField]
        GameObject effectPrefab;

        public int ID => EffectID;
    }

    public class EffectMasterContainer : IDBasedMasterContainer<EffectSettings>
    {
        protected override string resourcesFolder => "EffectSettings";
    }

    public class EffectMaster : Singleton<EffectMasterContainer>
    {
    }
}
