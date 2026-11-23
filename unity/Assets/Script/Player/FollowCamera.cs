using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

namespace AdvancedGears
{
    /// <summary>
    /// プレイヤーを追うカメラ。この GameObject（camTarget）の位置をカメラの目標位置とし、
    /// Camera.main を目標位置へ寄せつつ target（注視点）へ向ける。
    /// Play 中にインスペクターで値を変えるとすぐに反映される。値は FollowCameraEditor の「プレハブに反映」で保存する
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField]
        Transform center = null;

        [SerializeField]
        Transform target = null;

        [Header("注視点")]
        [SerializeField, Range(0.0f, 10.0f), Tooltip("注視点の高さ（m）。center（プレイヤーの足元）からの高さ")]
        float lookHeight = 0.0f;

        [SerializeField, Range(5.0f, 30.0f), Tooltip("注視点までの前方への距離（m）")]
        float targetLength = 5.0f;

        [Header("カメラの位置")]
        [SerializeField, Min(0.0f), Tooltip("カメラとプレイヤー（center）の距離（m）")]
        float cameraDistance = 17.0f;

        [SerializeField, Range(0.0f, 89.0f), Tooltip("カメラがプレイヤーを見下ろす角度（度）。上下の回転が 0 のときの値")]
        float cameraElevation = 5.0f;

        [Header("追従")]
        [SerializeField]
        float moveTime = 0.5f;

        [SerializeField, Range(0.0f, 30.0f), Tooltip("遊び（度）。カメラから見てプレイヤーがこの角度までずれても、カメラは追わない")]
        float deadZoneAngle = 1.0f;

        [SerializeField]
        float camDeltaRadius = 1.0f;

        [SerializeField]
        float powerRate = 2.0f;

        [SerializeField]
        float forwardDiffMin = 0.01f;

        [SerializeField]
        PlayerControllerSettings controllerSettings;

        float rotSpeed => controllerSettings.RotSpeed;
        float inputYMin => PlayerControllerConst.Instance.RotYMin;
        float rotLim => PlayerControllerConst.Instance.RotYLim;

        /// <summary>遊びを距離（m）に換算した値</summary>
        public float DeadZoneDistance => CalcDeadZoneDistance(cameraDistance, deadZoneAngle);

        /// <summary>上下の回転が 0 のときの、center から見たカメラの目標位置</summary>
        public Vector3 CameraLocalOffset => CalcCameraLocalOffset(cameraDistance, cameraElevation);

        float rotDeg = 0.0f;
        Camera cam;

        /// <summary>カメラと注視点の初期値（ResetCamera で戻す値）</summary>
        struct PlacementValues
        {
            public float lookHeight;
            public float targetLength;
            public float cameraDistance;
            public float cameraElevation;
        }

        PlacementValues initialValues;

        void Awake()
        {
            CaptureInitialValues();
        }

        /// <summary>
        /// 現在のカメラと注視点の値を、ResetCamera で戻す初期値として記録する。
        /// 起動時にプレハブの値を記録する。プレハブに値を反映したときにも呼ぶ
        /// </summary>
        public void CaptureInitialValues()
        {
            initialValues = new PlacementValues
            {
                lookHeight = lookHeight,
                targetLength = targetLength,
                cameraDistance = cameraDistance,
                cameraElevation = cameraElevation,
            };
        }

        /// <summary>
        /// カメラリセット。カメラと注視点の値をプレハブで設定された初期値に戻し、上下の回転を 0 にして、
        /// カメラを目標位置へすぐに移して注視点へ向ける
        /// </summary>
        public void ResetCamera()
        {
            lookHeight = initialValues.lookHeight;
            targetLength = initialValues.targetLength;
            cameraDistance = initialValues.cameraDistance;
            cameraElevation = initialValues.cameraElevation;

            rotDeg = 0.0f;
            UpdatePlacement();

            cam = cam ?? Camera.main;
            if (cam == null)
                return;

            var camTrans = cam.transform;
            camTrans.position = this.transform.position;
            var look = target.position - camTrans.position;
            if (look.sqrMagnitude > 0.0f)
                camTrans.forward = look.normalized;
        }

        /// <summary>
        /// center から見たカメラの位置。プレイヤーの後方に distance 離れ、elevationDeg だけ上から見下ろす
        /// </summary>
        public static Vector3 CalcCameraLocalOffset(float distance, float elevationDeg)
        {
            return Quaternion.AngleAxis(elevationDeg, Vector3.right) * (Vector3.back * distance);
        }

        /// <summary>
        /// 遊びの角度（度）を、カメラとプレイヤーの距離での長さ（m）に換算する
        /// </summary>
        public static float CalcDeadZoneDistance(float distance, float angleDeg)
        {
            var angle = Mathf.Clamp(angleDeg, 0.0f, 89.0f);
            return Mathf.Max(distance, 0.0f) * Mathf.Tan(angle * Mathf.Deg2Rad);
        }

        void FixedUpdate()
        {
            cam = cam ?? Camera.main;
            if (cam == null)
                return;

            UpdateCameraPos(cam.transform);
            UpdateCameraTarget(cam.transform);
            UpdateTarget();
        }

        private void UpdateCameraPos(Transform cam)
        {
            var current = cam.position;
            var target = this.transform.position;

            var diff = target - current;
            var diffMin = DeadZoneDistance;
            if (diff.sqrMagnitude <= diffMin * diffMin)
                return;

            diff -= diff.normalized * diffMin;

            var rate = (Time.deltaTime / moveTime) * Mathf.Pow((diff.magnitude / camDeltaRadius), powerRate);
            rate = Mathf.Min(rate, 1.0f);

            diff *= rate;
            cam.transform.position = current + diff;
        }

        private void UpdateTarget()
        {
            var mY = InputUtils.GetCenterMouse().y;

            bool canRotate = mY * mY >= inputYMin * inputYMin;

            if (mY > 0)
                canRotate &= rotDeg < rotLim;
            else
                canRotate &= rotDeg > -rotLim;

            if (canRotate)
            {
                rotDeg += mY * rotSpeed * Time.deltaTime;
            }

            UpdatePlacement();
        }

        /// <summary>上下の回転と現在の値から、カメラの目標位置（camTarget）と注視点（target）を置く</summary>
        void UpdatePlacement()
        {
            var quo = Quaternion.AngleAxis(-rotDeg, Vector3.right);
            var foward = quo * Vector3.forward;
            foward = center.localToWorldMatrix * foward;

            this.transform.localPosition = quo * CameraLocalOffset;

            target.position = GetLookBase() + foward * targetLength;
        }

        /// <summary>注視点の基準（center から lookHeight 上げた点）</summary>
        Vector3 GetLookBase()
        {
            return center.position + center.up * lookHeight;
        }

        private void UpdateCameraTarget(Transform cam)
        {
            var current = cam.position;
            var camFoward = cam.forward;

            var foward = target.position - current;
            var forwardDiff = foward - camFoward;
            if (forwardDiff.sqrMagnitude <= forwardDiffMin * forwardDiffMin)
                return;

            var tgt = camFoward * targetLength + current;
            var diff = target.position - tgt;
            diff *= Time.deltaTime / moveTime;
            tgt += diff;

            cam.forward = (tgt - current).normalized;
        }

        void OnDrawGizmosSelected()
        {
            if (center == null)
                return;

            // 注視点の基準と注視点
            var lookBase = GetLookBase();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lookBase, 0.3f);
            if (target != null)
            {
                Gizmos.DrawLine(lookBase, target.position);
                Gizmos.DrawWireSphere(target.position, 0.5f);
            }

            // カメラの目標位置と遊びの範囲
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(this.transform.position, DeadZoneDistance);
            Gizmos.DrawLine(center.position, this.transform.position);
        }
    }
}
