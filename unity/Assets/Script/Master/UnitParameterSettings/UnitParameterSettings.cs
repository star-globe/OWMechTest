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

        [Header("装甲")]
        [SerializeField]
        [Tooltip("装甲を持つか。持たないユニットは、装甲値ではなく「装甲なし」としてダメージを計算する（装甲値 0 とは区別する）")]
        bool hasArmor = true;
        public bool HasArmor => hasArmor;

        [SerializeField, Min(0)]
        [Tooltip("実弾装甲")]
        int physicalArmor = 0;

        [SerializeField, Min(0)]
        [Tooltip("EN 装甲")]
        int energyArmor = 0;

        /// <summary>属性ごとの装甲値（HasArmor が false なら使わない）</summary>
        public ArmorValues Armor => new ArmorValues(physicalArmor, energyArmor);

        [SerializeField]
        int bulletId = 1;
        public int BulletId => bulletId;

        /// <summary>発射間隔（秒）。0 以下なら撃たない</summary>
        [SerializeField]
        float fireInterval = 2.0f;
        public float FireInterval => fireInterval;

        /// <summary>同じ目標を狙い続けて二次ロック（偏差射撃）になるまでの時間（秒）。プレイヤーの銃の SecondaryLockTime に相当</summary>
        [SerializeField]
        float secondaryLockTime = 1.5f;
        public float SecondaryLockTime => secondaryLockTime;

        [Header("索敵")]
        /// <summary>索敵半径（m）</summary>
        [SerializeField]
        float searchRadius = 50.0f;
        public float SearchRadius => searchRadius;
    }
}
