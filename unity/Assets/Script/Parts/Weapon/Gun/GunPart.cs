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

        public void Fire(Vector3 target)
        {
            if (ReloadInter == int.MaxValue || ReloadInter <= 0)
                return;

            var now = TimeUtils.Now;
            var diff = now - fireDate;
            if (diff.TotalSeconds < (double)ReloadInter / ActionUtils.TimeDenominator)
                return;

            fireDate = now;

            BulletManager.Instance.Fire(BulletId, this.MuzzleTrans);
        }
    }
}
