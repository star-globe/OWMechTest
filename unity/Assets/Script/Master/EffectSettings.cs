using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Effect/EffectSettings", order = 0)]
    public class EffectSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField] int effectId;
        public int EffectID => effectId;
        public int ID => EffectID;

        [SerializeField] GameObject effectPrefab;
        public GameObject EffectPrefab => effectPrefab;

        [Tooltip("エフェクトの最大同時再生数。超えた分はスキップされる。")]
        [SerializeField] int maxPoolSize = 5;
        public int MaxPoolSize => maxPoolSize;
    }

    public class EffectMasterContainer : IDBasedMasterContainer<EffectSettings>
    {
        protected override string resourcesFolder => "EffectSettings";
    }

    public class EffectMaster : Singleton<EffectMasterContainer>
    {
    }
}
