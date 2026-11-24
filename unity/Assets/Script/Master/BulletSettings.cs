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
        [Tooltip("攻撃の属性（実弾 / EN）")]
        AttackAttribute attackAttribute = AttackAttribute.Physical;
        public AttackAttribute AttackAttribute => attackAttribute;

        /// <summary>命中したときに被弾側へ渡す攻撃の情報</summary>
        public AttackInfo AttackInfo => new AttackInfo(attack, attackAttribute);

        [SerializeField]
        int muzzleEffectId;
        public int MuzzleEffectId => muzzleEffectId;

        /// <summary>発射時に再生するエフェクトの ID（未設定なら標準のマズルフラッシュ）</summary>
        public int ResolvedMuzzleEffectId => ResolveMuzzleEffectId(muzzleEffectId);

        public static int ResolveMuzzleEffectId(int muzzleEffectId)
            => muzzleEffectId > 0 ? muzzleEffectId : EffectID.MuzzleFlash;

        [SerializeField]
        [Tooltip("命中したときに再生するエフェクトの ID（未設定なら標準の被弾スパーク）")]
        int hitEffectId;

        /// <summary>命中したときに再生するエフェクトの ID</summary>
        public int ResolvedHitEffectId => ResolveEffectId(hitEffectId, EffectID.HitSpark);

        [SerializeField]
        [Tooltip("撃ち落とされたときに再生するエフェクトの ID（未設定なら小爆発）。撃ち落とせる弾（Missile レイヤー）だけが使う")]
        int shootDownEffectId;

        /// <summary>撃ち落とされたときに再生するエフェクトの ID</summary>
        public int ResolvedShootDownEffectId => ResolveEffectId(shootDownEffectId, EffectID.ExplosionSmall);

        public static int ResolveEffectId(int effectId, int fallbackId)
            => effectId > 0 ? effectId : fallbackId;

        [Header("誘導（ミサイル）。旋回性能か誘導時間が 0 なら誘導しない")]
        [SerializeField, Min(0f)]
        [Tooltip("発射してから誘導が始まるまでの時間（秒）。その間はまっすぐ飛ぶ")]
        float guidanceDelay;
        public float GuidanceDelay => guidanceDelay;

        [SerializeField, Min(0f)]
        [Tooltip("誘導する時間（秒）。過ぎたらそのまま直進する")]
        float guidanceTime;
        public float GuidanceTime => guidanceTime;

        [SerializeField, Min(0f)]
        [Tooltip("旋回性能（1 秒あたりに曲がれる角度、度）")]
        float turnRate;
        public float TurnRate => turnRate;

        /// <summary>誘導する弾か</summary>
        public bool IsHoming => MissileGuidance.IsHoming(turnRate, guidanceTime);

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
