using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 固定砲台の索敵・砲塔回転を制御するコントローラー
    /// </summary>
    public class TurretController : MonoBehaviour
    {
        // ── SerializeField ───────────────────────────────
        [SerializeField]
        TurretUnit turretUnit;

        [SerializeField]
        Transform muzzle;

        [SerializeField]
        Transform cannon;

        // 索敵結果バッファ（GC 抑制用）
        private static readonly BaseObject[] searchBuffer = new BaseObject[32];

        // ── Unity Lifecycle ──────────────────────────────
        private void Update()
        {
            if (turretUnit == null || !turretUnit.IsAlive) return;

            var param = turretUnit.UnitParam;
            if (param == null) return;

            // 索敵
            if (TrySearchTarget(transform.position, param, out var targetPos))
            {
                // 目標がいた場合のみ Cannon を回転
                RotateCannon(targetPos, param);
            }
        }

        // ── 索敵 ─────────────────────────────────────────
        /// <summary>
        /// 自分と異なる UnitSide（UnitSide.None を除く）の最近傍 BaseObject を探す
        /// </summary>
        private bool TrySearchTarget(Vector3 pos, UnitParam param, out Vector3 targetPos)
        {
            targetPos = Vector3.zero;

            var count = PhysicsUtils.OverlapShpereOthers(
                pos,
                param.SearchRadius / GlobalParamMaster.Instance.WorldSizeRate,
                turretUnit.Side.ToNpcExcludeMask(),         // NPCは自陣営とNoneをチェックしない
                GameLayers.EnemyLayerMask,
                string.Empty,
                searchBuffer);

            float minSqrDist = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var obj = searchBuffer[i];
                if (obj == null) continue;

                var sqrDist = (obj.transform.position - pos).sqrMagnitude;
                if (sqrDist >= minSqrDist) continue;

                minSqrDist = sqrDist;
                targetPos = obj.transform.position;
                found = true;
            }

            return found;
        }

        // ── Cannon 回転 ───────────────────────────────────
        /// <summary>
        /// Cannon を目標方向に向けて、UnitParam で指定された速度で回転させる
        /// Y軸（水平）・X軸（俯仰）をそれぞれ独立に補間し、仰俯角を ElevationMin/Max でクランプする
        /// </summary>
        private void RotateCannon(Vector3 targetPos, UnitParam param)
        {
            if (cannon == null) return;

            var dirToTarget = targetPos - cannon.position;

            // 親が存在する場合は親のローカル空間に変換する。
            // これにより親オブジェクトの回転に追従した正しい角度計算が可能になる。
            var parent = cannon.parent;
            var localDir = parent != null
                ? parent.InverseTransformDirection(dirToTarget)
                : dirToTarget;

            // ローカル空間での XZ 平面への投影（水平方向）
            var flatDir = new Vector3(localDir.x, 0f, localDir.z);
            if (flatDir.sqrMagnitude < 0.0001f) return;

            var deltaTime = Time.deltaTime;

            // ── Y 軸（水平）回転 ──────────────────────────
            var targetYaw  = Quaternion.LookRotation(flatDir, Vector3.up).eulerAngles.y;
            var currentYaw = cannon.localEulerAngles.y;
            var newYaw     = Mathf.MoveTowardsAngle(currentYaw, targetYaw,
                                 param.TurretRotationSpeedY * deltaTime);

            // ── X 軸（俯仰）回転 ──────────────────────────
            // ローカル空間での水平距離と高さの差から目標仰俯角を算出（下向きが負）
            var horizDist   = flatDir.magnitude;
            var targetPitch = -Mathf.Atan2(localDir.y, horizDist) * Mathf.Rad2Deg;
            targetPitch     = Mathf.Clamp(targetPitch, param.ElevationMin, param.ElevationMax);

            // localEulerAngles は 0–360 で返るので 180 超は負に変換
            var currentPitch = cannon.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;

            var newPitch = Mathf.MoveTowardsAngle(currentPitch, targetPitch,
                               param.TurretRotationSpeedX * deltaTime);

            // ローカル空間に適用（Z 軸は変化なし）
            cannon.localEulerAngles = new Vector3(newPitch, newYaw, 0f);
        }
    }
}
