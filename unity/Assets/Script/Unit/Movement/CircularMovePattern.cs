using UnityEngine;

namespace AdvancedGears
{
    /// <summary>円を描く面</summary>
    public enum CirclePlane
    {
        /// <summary>水平面（地上・空中）</summary>
        Horizontal = 0,
        /// <summary>縦の面（空中のみ）。面の向きは planeYaw で決める</summary>
        Vertical = 1,
    }

    /// <summary>円運動の回転方向</summary>
    public enum CircleDirection
    {
        /// <summary>水平面: 上から見て時計回り。縦の面: 面の前方向 → 上 → 後ろ → 下の順</summary>
        Forward = 0,
        /// <summary>Forward の逆回り</summary>
        Reverse = 1,
    }

    /// <summary>
    /// 等速の円運動。角度 θ は面の前方向（水平面ならスポーン時の正面、縦の面なら planeYaw だけ回した正面）を 0 とし、
    /// 位置 = 中心 + 半径 × (cosθ × 前方向 + sinθ × 横方向) で求める（横方向は水平面なら右、縦の面なら上）
    /// </summary>
    public class CircularMovePattern : IUnitMovePattern
    {
        readonly Vector3 center;
        readonly float radius;
        readonly float angularSpeed;   // rad/s（回転方向の符号込み）
        readonly float startAngle;     // rad
        readonly Vector3 axisU;        // θ = 0 の方向
        readonly Vector3 axisV;        // θ = 90 度の方向

        /// <param name="center">中心（スポーン位置からのオフセット）</param>
        /// <param name="radius">半径</param>
        /// <param name="speed">周速（距離/秒）</param>
        /// <param name="direction">回転方向</param>
        /// <param name="startAngleDeg">開始時の角度（度）</param>
        /// <param name="plane">円を描く面</param>
        /// <param name="planeYawDeg">縦の面の向き（スポーン時の正面から右回りの角度、度）。水平面では使わない</param>
        public CircularMovePattern(Vector3 center, float radius, float speed, CircleDirection direction,
                                   float startAngleDeg, CirclePlane plane, float planeYawDeg)
        {
            this.center = center;
            this.radius = Mathf.Max(radius, 0f);
            var sign = direction == CircleDirection.Forward ? 1f : -1f;
            angularSpeed = this.radius > 0f ? sign * speed / this.radius : 0f;
            startAngle = startAngleDeg * Mathf.Deg2Rad;

            if (plane == CirclePlane.Vertical)
            {
                axisU = Quaternion.Euler(0f, planeYawDeg, 0f) * Vector3.forward;
                axisV = Vector3.up;
                HasFixedForward = true;
                FixedForward = axisU;
            }
            else
            {
                axisU = Vector3.forward;
                axisV = Vector3.right;
                HasFixedForward = false;
                FixedForward = Vector3.forward;
            }
        }

        public bool HasFixedForward { get; }
        public Vector3 FixedForward { get; }

        public void Evaluate(float time, out Vector3 localPosition, out Vector3 localVelocity)
        {
            var theta = startAngle + angularSpeed * time;
            var cos = Mathf.Cos(theta);
            var sin = Mathf.Sin(theta);

            localPosition = center + radius * (cos * axisU + sin * axisV);
            localVelocity = radius * angularSpeed * (-sin * axisU + cos * axisV);
        }
    }
}
