using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    public enum BulletCategoryType
    {
        None = 0,
        Normal,

        Normal_AntiObject = 99,
    }

    [CreateAssetMenu(menuName = "TestProject/Bullet/BulletSettings", order = 0)]
    public class BulletSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField]
        int bulletId;
        public int BulletID => bulletId;

        [SerializeField]
        BulletCategoryType bulletCategoryType;
        public BulletCategoryType BulletCategoryType => bulletCategoryType;

        [SerializeField]
        int speed;
        public int Speed => speed;

        [SerializeField]
        int lifeTime;
        public int LifeTime => lifeTime;

        [SerializeField]
        int attack;
        public int Attack => attack;

        [SerializeField]
        int muzzleEffectId;
        public int MuzzleEffectId => muzzleEffectId;

        [SerializeField]
        Bullet bulletComp;

        public Bullet InstantiateBullet()
        {
            Bullet inst = null;
            if (bulletComp != null)
            {
                inst = Instantiate(bulletComp);
                inst.SetInfo(bulletId);
            }

            return inst;
        }

        public int ID => BulletID;
    }

    public class BulletMasterContainer : IDBasedMasterContainer<BulletSettings>
    {
        protected override string resourcesFolder => "BulletSettings";
    }

    public class BulletMaster : Singleton<BulletMasterContainer>
    {
        public static Bullet InstantiateBullet(int bulletId)
        {
            return Instance.GetSettings(bulletId)?.InstantiateBullet();
        }
    }
}
