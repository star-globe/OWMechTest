using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField]
        TrailRenderer trail;

        [SerializeField]
        Rigidbody rigid;

        private void Awake()
        {
            Assert.IsNotNull(rigid);
            Assert.IsNotNull(trail);
        }

        public int BulletId { get; private set; }
        public long InstanceId { get; private set; }
        public DateTime DeadTime { get; private set; }

        public void SetInfo(int bulletId)
        {
            BulletId = bulletId;
        }

        private BulletSettings Settings => BulletMaster.Instance.GetSettings(BulletId);

        public BulletCategoryType Category => Settings == null ? BulletCategoryType.None : Settings.BulletCategoryType;
        public float VecSpeed => Settings == null ? 0.0f : Settings.Speed;
        public float LifeTime => Settings == null ? 0.0f : Settings.LifeTime;
        public int Attack => Settings == null ? 0 : Settings.Attack;

        /// <summary>
        /// 発射する。初速は「direction × 弾速 + inheritVelocity（撃った側の速度）」
        /// </summary>
        public void Fire(Vector3 direction, Vector3 pos, Vector3 inheritVelocity, long instanceId)
        {
            var vec = direction.normalized * this.VecSpeed + inheritVelocity;

            this.transform.position = pos;
            if (vec.sqrMagnitude > 0f)
                this.transform.forward = vec.normalized;

            trail.Clear();
            trail.AddPosition(pos);
            this.gameObject.SetActive(true);

            // プールから再利用したときに前の速度が残らないよう、加算ではなく代入する
            rigid.position = pos;
            rigid.linearVelocity = vec;
            rigid.angularVelocity = Vector3.zero;

            this.InstanceId = instanceId;
            this.DeadTime = TimeUtils.Now + TimeSpan.FromSeconds(this.LifeTime);
        }

        public void Vanish()
        {
            this.gameObject.SetActive(false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & GameLayers.HitLayerMask) != 0)
            {
                var receiver = collision.gameObject.GetComponent<HitReceiver>();
                receiver?.Damage(Attack);
                EffectManager.Instance?.PlayEffect(EffectID.HitSpark, transform.position, Quaternion.LookRotation(collision.contacts[0].normal));
            }
            else
            {
                EffectManager.Instance?.PlayEffect(EffectID.HitSpark, transform.position);
            }

            Vanish();
        }
    }
}
