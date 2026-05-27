using NUnit.Framework;
using System;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Global/GlobalParamSettings", order = 0)]
    public class GlobalParamSettings : ScriptableObject
    {
        [SerializeField]
        float worldSizeRate = 5.0f;
        public float WorldSizeRate
        {
            get
            {
                Assert.IsTrue(worldSizeRate > 1.0f);

                return worldSizeRate;
            }
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
