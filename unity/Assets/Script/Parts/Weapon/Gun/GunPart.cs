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

    /// <summary>
    /// 弾を撃つパーツ。腕の武器（ライフル）と、サブ装備のうち射出するもの（ミサイル）で使う。
    /// 武器のマスタの一斉発射の数が 2 以上なら、1 回の射撃で決めた間隔をあけて順に撃つ（ロックの目標と段階は全弾で同じ）
    /// </summary>
    public class GunPart : PartComponent, ISubPart
    {
        [SerializeField]
        [Tooltip("一斉発射で順に使う発射口（未設定なら右用・左用の発射位置だけを使う）")]
        Transform[] burstMuzzles = null;

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

        readonly BurstSchedule burst = new BurstSchedule();

        /// <summary>一斉発射の残りの弾で使う射撃の要求（発射時のロックの目標と段階を全弾で使う）</summary>
        AimRequest burstRequest;

        /// <summary>次に使う発射口（burstMuzzles の番号）</summary>
        int burstMuzzleIndex;

        public void Fire(in AimRequest request)
        {
            if (ReloadInter == int.MaxValue || ReloadInter <= 0)
                return;

            // 一斉発射の途中は次の射撃を受け付けない
            if (burst.IsActive)
                return;

            var now = TimeUtils.Now;
            var diff = now - fireDate;
            if (diff.TotalSeconds < (double)ReloadInter / ActionUtils.TimeDenominator)
                return;

            fireDate = now;

            var settings = Settings;
            burstRequest = request;
            burstMuzzleIndex = 0;
            burst.Start(settings != null ? settings.BurstCount : 1, settings != null ? settings.BurstInterval : 0f, Time.timeAsDouble);
            FireDueShots();
        }

        /// <summary>サブ装備として使う（撃つ）</summary>
        public void Use(in AimRequest request) => Fire(request);

        private void Update()
        {
            if (burst.IsActive)
                FireDueShots();
        }

        private void OnDisable()
        {
            // 撃破などで無効になったら、残りの弾は撃たない
            burst.Cancel();
            burstRequest = default;
        }

        void FireDueShots()
        {
            var count = burst.ConsumeDue(Time.timeAsDouble);
            for (int i = 0; i < count; i++)
                FireOne(burstRequest);

            if (!burst.IsActive)
                burstRequest = default;
        }

        void FireOne(in AimRequest request)
        {
            var muzzle = NextMuzzle();
            var muzzlePos = muzzle.position;
            var dir = AimSolver.CalcAimDirection(request, muzzlePos, BulletSpeed, muzzle.forward);
            BulletManager.Instance.Fire(BulletId, muzzlePos, dir, request.ShooterVelocity, request.Shooter, BulletGuidance.From(request));
        }

        Transform NextMuzzle()
        {
            if (burstMuzzles == null || burstMuzzles.Length == 0)
                return MuzzleTrans;

            var muzzle = burstMuzzles[burstMuzzleIndex % burstMuzzles.Length];
            burstMuzzleIndex++;
            return muzzle != null ? muzzle : MuzzleTrans;
        }
    }
}
