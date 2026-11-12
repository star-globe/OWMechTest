using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 偏差射撃の照準点の計算（Unity のシーンに依存しない）。
    /// 弾は「照準方向 × 弾速 + 撃った側の速度」で等速直線運動し、目標も等速で動くと仮定する（加速度・重力は考えない）。
    /// 撃った側から見た相対運動で、|D + V t| = s t を解く（D = 目標 - マズル、V = 目標の速度 - 撃った側の速度、s = 弾速）
    /// </summary>
    public static class LeadCalculator
    {
        const float Epsilon = 1e-6f;

        /// <summary>
        /// 照準点を求める。弾が目標に追いつけない（解がない）ときは false を返し、aimPoint は目標の現在位置にする
        /// </summary>
        /// <param name="muzzlePos">マズルの位置</param>
        /// <param name="shooterVelocity">撃った側の速度（弾が引き継ぐ）</param>
        /// <param name="targetPos">目標の位置</param>
        /// <param name="targetVelocity">目標の速度（一次ロックでは 0 を渡す）</param>
        /// <param name="bulletSpeed">弾速</param>
        /// <param name="aimPoint">照準点（マズルからこの点へ向けて撃つ）</param>
        public static bool TryCalcAimPoint(Vector3 muzzlePos, Vector3 shooterVelocity, Vector3 targetPos, Vector3 targetVelocity,
                                           float bulletSpeed, out Vector3 aimPoint)
        {
            aimPoint = targetPos;

            if (TryCalcHitTime(targetPos - muzzlePos, targetVelocity - shooterVelocity, bulletSpeed, out var t) == false)
                return false;

            aimPoint = targetPos + (targetVelocity - shooterVelocity) * t;
            return true;
        }

        /// <summary>
        /// 相対位置 d・相対速度 v の目標に、速さ s の弾が当たるまでの時間 t（正の最小解）を求める
        /// </summary>
        public static bool TryCalcHitTime(Vector3 d, Vector3 v, float s, out float t)
        {
            t = 0f;
            if (s <= 0f)
                return false;

            var dd = Vector3.Dot(d, d);
            if (dd < Epsilon)
                return true; // すでに重なっている

            // (v·v - s²) t² + 2 (d·v) t + d·d = 0
            var a = Vector3.Dot(v, v) - s * s;
            var b = 2f * Vector3.Dot(d, v);
            var c = dd;

            if (Mathf.Abs(a) < Epsilon)
            {
                // 相対速度と弾速が等しい：一次方程式 b t + c = 0
                if (b >= 0f)
                    return false;

                t = -c / b;
                return t > 0f;
            }

            var disc = b * b - 4f * a * c;
            if (disc < 0f)
                return false;

            var sqrt = Mathf.Sqrt(disc);
            var t1 = (-b - sqrt) / (2f * a);
            var t2 = (-b + sqrt) / (2f * a);

            var min = Mathf.Min(t1, t2);
            var max = Mathf.Max(t1, t2);
            if (min > 0f)
                t = min;
            else if (max > 0f)
                t = max;
            else
                return false;

            return true;
        }
    }
}
