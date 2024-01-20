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

                if (queue.Count > 0)
                    queue.Dequeue();

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

        public void Fire(int bulletId, Transform muzzleTrans)
        {
            if (muzzleTrans == null)
            {
                Debug.LogErrorFormat("There is no MuzzleTransform. BulletId:{0}", bulletId);
                return;
            }

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

            bullet.Fire(muzzleTrans.forward, muzzleTrans.position, instanceId);

            currentBullets[instanceId] = bullet;
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
