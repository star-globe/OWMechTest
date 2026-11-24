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

        // 装甲の基準値。装甲値がこの値と同じとき、ダメージは半分になる（DamageCalculator）
        [SerializeField, Min(1)]
        [Tooltip("装甲の基準値。ダメージ = 攻撃力 × 基準値 ÷ (基準値 + 装甲値)。装甲値がこの値と同じとき、ダメージは半分になる")]
        int armorReferenceValue = 1000;
        public int ArmorReferenceValue => armorReferenceValue;

        private void OnValidate()
        {
            Assert.IsTrue(worldSizeRate > 0.0f);
            Assert.IsTrue(armorReferenceValue > 0);
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

        public int ArmorReferenceValue => Settings.ArmorReferenceValue;

        public void Load()
        {
            settings = Resources.Load<GlobalParamSettings>(resourcesPath);
        }
    }
}
