using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BulletManager : SingletonMonoBehaviour<BulletManager>
    {
        class BulletQueue
        {
            readonly Dictionary<int, Queue<Bullet>> bulletQueue = new Dictionary<int, Queue<Bullet>>();

            public Bullet DequeueBullet(int bulletId)
            {
                if (bulletQueue.TryGetValue(bulletId, out var queue) == false)
                    return null;

                // シーンの切り替えなどで破棄された弾は飛ばす
                while (queue.Count > 0)
                {
                    var bullet = queue.Dequeue();
                    if (bullet != null)
                        return bullet;
                }

                return null;
            }

            public void EnqueueBullet(Bullet bullet)
            {
                if (bullet == null)
                    return;

                var bulletId = bullet.BulletId;
                if (bulletQueue.ContainsKey(bulletId) == false)
                    bulletQueue[bulletId] = new Queue<Bullet>();

                bulletQueue[bulletId].Enqueue(bullet);
            }
        }

        readonly BulletQueue bulletQueue = new BulletQueue();
        readonly Dictionary<long, Bullet> currentBullets = new Dictionary<long, Bullet>();
        readonly List<long> vanishKeys = new List<long>();

        long instanceId = 1;

        /// <summary>マズルの正面へ撃つ（撃った側の速度は引き継がない）</summary>
        public void Fire(int bulletId, Transform muzzleTrans)
        {
            if (muzzleTrans == null)
            {
                Debug.LogErrorFormat("There is no MuzzleTransform. BulletId:{0}", bulletId);
                return;
            }

            Fire(bulletId, muzzleTrans.position, muzzleTrans.forward, Vector3.zero);
        }

        /// <summary>
        /// position から direction へ撃つ。弾の初速は「direction × 弾速 + inheritVelocity（撃った側の速度）」。
        /// shooter（撃った側）には当たらない
        /// </summary>
        public void Fire(int bulletId, Vector3 position, Vector3 direction, Vector3 inheritVelocity, BaseObject shooter = null)
        {
            var bullet = bulletQueue.DequeueBullet(bulletId);
            if (bullet == null)
            {
                bullet = BulletMaster.InstantiateBullet(bulletId);
            }

            if (bullet == null)
            {
                return;
            }

            instanceId++;

            bullet.Fire(direction, position, inheritVelocity, instanceId, shooter);

            currentBullets[instanceId] = bullet;

            PlayMuzzleEffect(bulletId, position, direction);
        }

        /// <summary>マズルの位置に、発射の向き（+Z）でマズルフラッシュを出す</summary>
        void PlayMuzzleEffect(int bulletId, Vector3 position, Vector3 direction)
        {
            var settings = BulletMaster.Instance.GetSettings(bulletId);
            if (settings == null)
                return;

            var rotation = direction.sqrMagnitude > 0f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            EffectManager.Instance?.PlayEffect(settings.ResolvedMuzzleEffectId, position, rotation);
        }

        public void Vanish(long instanceId)
        {
            if (currentBullets.ContainsKey(instanceId) == false)
                return;

            var bullet = currentBullets[instanceId];
            if (bullet != null)
            {
                bullet.Vanish();
                bulletQueue.EnqueueBullet(bullet);
            }

            currentBullets.Remove(instanceId);
        }

        private void Update()
        {
            vanishKeys.Clear();

            foreach (var kvp in currentBullets)
            {
                if (kvp.Value.DeadTime < TimeUtils.Now)
                    vanishKeys.Add(kvp.Key);
            }

            foreach (var key in vanishKeys)
            {
                Vanish(key);
            }
        }
    }
}
