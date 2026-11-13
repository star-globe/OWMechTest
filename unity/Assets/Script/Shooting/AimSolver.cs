using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 射撃の要求。撃つ側（プレイヤー・NPC・敵ユニット）が作り、武器に渡す
    /// </summary>
    public readonly struct AimRequest
    {
        /// <summary>ロックしている目標（ロックしていなければ null）</summary>
        public readonly BaseObject Target;
        /// <summary>この武器でのロックの段階</summary>
        public readonly LockLevel Level;
        /// <summary>ロックしていないときに狙う点（正面など）</summary>
        public readonly Vector3 FallbackPoint;
        /// <summary>撃つ側の速度（弾が引き継ぐ）</summary>
        public readonly Vector3 ShooterVelocity;
        /// <summary>撃つ側（弾は撃った側に当たらない。null なら除外しない）</summary>
        public readonly BaseObject Shooter;

        public AimRequest(BaseObject target, LockLevel level, Vector3 fallbackPoint, Vector3 shooterVelocity, BaseObject shooter = null)
        {
            Target = target;
            Level = level;
            FallbackPoint = fallbackPoint;
            ShooterVelocity = shooterVelocity;
            Shooter = shooter;
        }

        public bool IsLocked => Level != LockLevel.None && Target != null;
    }

    /// <summary>
    /// ロックの段階に応じて照準点を決める。
    /// 一次ロックは目標の速度を 0、二次ロックは目標の実際の速度として LeadCalculator で計算する
    /// </summary>
    public static class AimSolver
    {
        /// <summary>ロックの段階・目標の位置と速度から照準点を求める（Unity のシーンに依存しない）</summary>
        public static Vector3 CalcAimPoint(LockLevel level, Vector3 muzzlePos, Vector3 shooterVelocity, float bulletSpeed,
                                           Vector3 targetPos, Vector3 targetVelocity, Vector3 fallbackPoint)
        {
            if (level == LockLevel.None)
                return fallbackPoint;

            var velocity = level == LockLevel.Secondary ? targetVelocity : Vector3.zero;

            // 追いつけないときは LeadCalculator が目標の現在位置を返す
            LeadCalculator.TryCalcAimPoint(muzzlePos, shooterVelocity, targetPos, velocity, bulletSpeed, out var aimPoint);
            return aimPoint;
        }

        public static Vector3 CalcAimPoint(in AimRequest request, Vector3 muzzlePos, float bulletSpeed)
        {
            if (request.IsLocked == false)
                return request.FallbackPoint;

            var target = request.Target;
            return CalcAimPoint(request.Level, muzzlePos, request.ShooterVelocity, bulletSpeed,
                                target.AimPosition, target.Velocity, request.FallbackPoint);
        }

        /// <summary>照準点への発射方向（単位ベクトル）。照準点がマズルと重なるときは fallbackDir</summary>
        public static Vector3 CalcAimDirection(in AimRequest request, Vector3 muzzlePos, float bulletSpeed, Vector3 fallbackDir)
        {
            var dir = CalcAimPoint(request, muzzlePos, bulletSpeed) - muzzlePos;
            return dir.sqrMagnitude > 1e-6f ? dir.normalized : fallbackDir;
        }
    }
}
