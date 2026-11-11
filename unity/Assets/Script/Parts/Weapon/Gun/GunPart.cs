using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class GunPart : PartComponent
    {
        [SerializeField]
        Transform muzzleTrans = null;
        public Transform MuzzleTrans => muzzleTrans;

        private void Awake()
        {
            Assert.IsNotNull(muzzleTrans);
        }

        private DateTime fireDate = DateTime.MinValue;

        private GunSettings Settings => GunMaster.Instance.GetSettings(this.PartId);

        public int BulletId => Settings == null ? 0: Settings.BulletID;
        public int ReloadInter => Settings == null ? int.MaxValue : Settings.ReloadInter;

        /// <summary>二次ロックになるまでの時間（秒）</summary>
        public float SecondaryLockTime => AdvancedGears.SecondaryLockTime.Calc(Settings);

        float BulletSpeed
        {
            get
            {
                var bullet = BulletMaster.Instance.GetSettings(BulletId);
                return bullet == null ? 0f : bullet.Speed;
            }
        }

        public void Fire(in AimRequest request)
        {
            if (ReloadInter == int.MaxValue || ReloadInter <= 0)
                return;

            var now = TimeUtils.Now;
            var diff = now - fireDate;
            if (diff.TotalSeconds < (double)ReloadInter / ActionUtils.TimeDenominator)
                return;

            fireDate = now;

            var muzzlePos = muzzleTrans.position;
            var dir = AimSolver.CalcAimDirection(request, muzzlePos, BulletSpeed, muzzleTrans.forward);
            BulletManager.Instance.Fire(BulletId, muzzlePos, dir, request.ShooterVelocity);
        }
    }
}
