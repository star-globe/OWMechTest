using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    public enum GunCategoryType
    {
        None = 0,
        Rifle,
        Missile,
    }

    [CreateAssetMenu(menuName = "TestProject/Parts/GunSettings", order = 0)]
    public class GunSettings : PartSettings
    {
        [SerializeField]
        GunCategoryType gunCategoryType;
        public GunCategoryType GunCategoryType => gunCategoryType;

        [SerializeField]
        int bulletId;
        public int BulletID => bulletId;

        [SerializeField]
        int reloadInter;
        public int ReloadInter
        {
            get
            {
                var limit = ActionUtils.ReloadInterLimit;
                if (reloadInter > limit)
                    return reloadInter;
                return limit;
            }
        }

        [SerializeField]
        [Tooltip("一次ロックから二次ロック（偏差射撃）になるまでの時間（秒）")]
        float secondaryLockTime = 1.0f;
        public float SecondaryLockTime => secondaryLockTime;

        [SerializeField, Min(1)]
        [Tooltip("1 回の射撃で撃つ数（一斉発射）。1 なら 1 発だけ撃つ")]
        int burstCount = 1;
        public int BurstCount => Mathf.Max(1, burstCount);

        [SerializeField, Min(0f)]
        [Tooltip("一斉発射の中で、次の弾を撃つまでの間隔（秒）")]
        float burstInterval = 0f;
        public float BurstInterval => Mathf.Max(0f, burstInterval);
    }

    public class GunMasterContainer : IDBasedMasterContainer<GunSettings>
    {
        protected override string resourcesFolder => "GunSettings";
    }

    public class GunMaster : Singleton<GunMasterContainer>
    {
        public static GunPart InstantiateGunPart(int gunId)
        {
            // 負の ID は未装備
            if (gunId < 0)
                return null;

            return Instance.GetSettings(gunId)?.InstantiatePart<GunPart>();
        }
    }
}
