using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニット共通パラメーター設定の基底 ScriptableObject
    /// </summary>
    public abstract class UnitParameterSettings : ScriptableObject
    {
        [Header("共通パラメーター")]
        [SerializeField]
        int maxAp = 500;
        public int MaxAp => maxAp;

        [SerializeField]
        int bulletId = 1;
        public int BulletId => bulletId;

        /// <summary>発射間隔（秒）</summary>
        [SerializeField]
        float fireInterval = 2.0f;
        public float FireInterval => fireInterval;
    }
}
