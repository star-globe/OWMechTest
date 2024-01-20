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
    }

    public class GunMasterContainer : IDBasedMasterContainer<GunSettings>
    {
        protected override string resourcesFolder => "GunSettings";
    }

    public class GunMaster : Singleton<GunMasterContainer>
    {
        public static GunPart InstantiateGunPart(int gunId)
        {
            return Instance.GetSettings(gunId)?.InstantiatePart<GunPart>();
        }
    }
}
