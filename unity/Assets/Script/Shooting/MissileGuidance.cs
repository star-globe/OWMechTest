using UnityEngine;

namespace AdvancedGears
{
    /// <summary>誘導の段階</summary>
    public enum GuidancePhase
    {
        /// <summary>誘導が始まる前（まっすぐ飛ぶ）</summary>
        BeforeGuidance = 0,
        /// <summary>目標へ曲がる</summary>
        Guiding,
        /// <summary>誘導しない（ロックなしで撃った・誘導時間が過ぎた・目標を失った）。まっすぐ飛ぶ</summary>
        Straight,
    }

    /// <summary>
    /// ミサイルの誘導の計算（Unity のシーンに依存しない）。
    /// 誘導が始まるまでの時間はまっすぐ飛び、その後は誘導時間の間、旋回性能（度/秒）の上限つきで目標へ向きを変える
    /// </summary>
    public static class MissileGuidance
    {
        /// <summary>誘導するパラメーターか（旋回性能と誘導時間が正）</summary>
        public static bool IsHoming(float turnRate, float guidanceTime)
        {
            return turnRate > 0f && guidanceTime > 0f;
        }

        /// <summary>
        /// 発射からの経過時間で、誘導の段階を返す。目標が無い・失ったときは Straight
        /// </summary>
        /// <param name="elapsed">発射からの経過時間（秒）</param>
        /// <param name="guidanceDelay">誘導が始まるまでの時間（秒）</param>
        /// <param name="guidanceTime">誘導時間（秒）</param>
        /// <param name="hasTarget">追う目標があるか</param>
        public static GuidancePhase GetPhase(float elapsed, float guidanceDelay, float guidanceTime, bool hasTarget)
        {
            if (hasTarget == false)
                return GuidancePhase.Straight;

            if (elapsed < guidanceDelay)
                return GuidancePhase.BeforeGuidance;

            if (elapsed < guidanceDelay + guidanceTime)
                return GuidancePhase.Guiding;

            return GuidancePhase.Straight;
        }

        /// <summary>
        /// 目標を失ったか。破棄された・非アクティブ・AP が 0 以下（撃破）なら true。
        /// AP を持たない目標（maxAp が int.MinValue）は AP で判定しない
        /// </summary>
        public static bool IsTargetLost(bool exists, bool isActive, int ap, int maxAp)
        {
            if (exists == false || isActive == false)
                return true;

            return maxAp != int.MinValue && ap <= 0;
        }

        /// <summary>
        /// 向かう点。一次ロックは目標の現在位置、二次ロックは目標の速度から未来位置を予測する（偏差射撃と同じ考え方）。
        /// 追いつけないときは現在位置
        /// </summary>
        public static Vector3 CalcAimPoint(LockLevel level, Vector3 missilePos, float missileSpeed, Vector3 targetPos, Vector3 targetVelocity)
        {
            if (level != LockLevel.Secondary)
                return targetPos;

            LeadCalculator.TryCalcAimPoint(missilePos, Vector3.zero, targetPos, targetVelocity, missileSpeed, out var aimPoint);
            return aimPoint;
        }

        /// <summary>
        /// 今の向きから目指す向きへ、1 秒あたり turnRate 度を上限に向きを変える（どちらも単位ベクトルでなくてよい）。
        /// 戻り値は単位ベクトル
        /// </summary>
        public static Vector3 TurnTowards(Vector3 currentDir, Vector3 desiredDir, float turnRate, float deltaTime)
        {
            if (currentDir.sqrMagnitude < 1e-8f)
                return desiredDir.sqrMagnitude < 1e-8f ? Vector3.forward : desiredDir.normalized;

            var current = currentDir.normalized;
            if (desiredDir.sqrMagnitude < 1e-8f)
                return current;

            var maxRadians = Mathf.Max(0f, turnRate) * Mathf.Deg2Rad * Mathf.Max(0f, deltaTime);
            return Vector3.RotateTowards(current, desiredDir.normalized, maxRadians, 0f).normalized;
        }
    }
}
