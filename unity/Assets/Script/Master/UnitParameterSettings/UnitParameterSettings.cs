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
