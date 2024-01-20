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

        public void Fire(Vector3 vec, Vector3 pos, long instanceId)
        {
            vec = vec.normalized * this.VecSpeed;

            trail.Clear();
            trail.AddPosition(pos);
            rigid.AddForce(vec, ForceMode.VelocityChange);
            this.gameObject.SetActive(true);

            this.transform.position = pos;
            this.transform.forward = vec.normalized;

            this.InstanceId = instanceId;
            this.DeadTime = TimeUtils.Now + TimeSpan.FromSeconds(this.LifeTime);
        }

        public void Vanish()
        {
            this.gameObject.SetActive(false);
        }

        const string tagStr = "HitObject";
        private void OnCollisionEnter(Collision collision)
        {
            if (string.Equals(collision.gameObject.tag, tagStr))
            {
                var receiver = collision.gameObject.GetComponent<HitReceiver>();
                if (receiver != null)
                {
                    receiver.Damage(Attack);
                }
            }

            Vanish();
        }
    }
}
