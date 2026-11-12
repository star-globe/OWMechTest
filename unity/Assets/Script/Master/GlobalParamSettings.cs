using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Global/GlobalParamSettings", order = 0)]
    public class GlobalParamSettings : ScriptableObject
    {
        // Unity の 1 単位がゲーム内の何 m にあたるか。機体のモデルを 1 単位 = 1 m（全高 8 m）で作っているので 1
        [SerializeField]
        float worldSizeRate = 1.0f;
        public float WorldSizeRate
        {
            get
            {
                return worldSizeRate;
            }
        }

        private void OnValidate()
        {
            Assert.IsTrue(worldSizeRate > 0.0f);
        }
    }

    public class GlobalParamMaster : Singleton<GlobalParamMaster>
    {
        const string resourcesPath = "GlobalParamSettings";

        GlobalParamSettings settings = null;

        private GlobalParamSettings Settings
        {
            get
            {
                if (settings == null)
                    throw new Exception("GlobalParamSettings is Null!");

                return settings;
            }
        }

        public float WorldSizeRate => Settings.WorldSizeRate;

        public void Load()
        {
            settings = Resources.Load<GlobalParamSettings>(resourcesPath);
        }
    }
}
