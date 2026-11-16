using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace AdvancedGears
{
    /// <summary>銃を持つ側。左右で鏡像のモデルを使い分ける</summary>
    public enum GunSide
    {
        Right = 0,
        Left,
    }

    public class GunPart : PartComponent
    {
        [SerializeField]
        [FormerlySerializedAs("muzzleTrans")]
        Transform rightMuzzle = null;

        [SerializeField]
        [Tooltip("右用のモデル（未設定なら切り替えない）")]
        GameObject rightModel = null;

        [SerializeField]
        [Tooltip("左用の発射位置（未設定なら右用を使う）")]
        Transform leftMuzzle = null;

        [SerializeField]
        [Tooltip("左用のモデル（未設定なら右用のモデルを左でも使う）")]
        GameObject leftModel = null;

        GunSide side = GunSide.Right;
        public GunSide Side => side;

        bool HasLeft => leftModel != null && leftMuzzle != null;

        public Transform MuzzleTrans => side == GunSide.Left && HasLeft ? leftMuzzle : rightMuzzle;

        private void Awake()
        {
            Assert.IsNotNull(rightMuzzle);
            ApplySide();
        }

        /// <summary>持つ側を切り替え、その側のモデルと発射位置を使う</summary>
        public void SetSide(GunSide side)
        {
            this.side = side;
            ApplySide();
        }

        void ApplySide()
        {
            if (!HasLeft)
                return;

            bool isLeft = side == GunSide.Left;
            leftModel.SetActive(isLeft);
            if (rightModel != null)
                rightModel.SetActive(!isLeft);
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

            var muzzle = MuzzleTrans;
            var muzzlePos = muzzle.position;
            var dir = AimSolver.CalcAimDirection(request, muzzlePos, BulletSpeed, muzzle.forward);
            BulletManager.Instance.Fire(BulletId, muzzlePos, dir, request.ShooterVelocity, request.Shooter);
        }
    }
}
