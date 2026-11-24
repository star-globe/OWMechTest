using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクト確認シーンのカメラ。右ドラッグで目標の周りを回り込み、ホイールで近づく・離れる
    /// </summary>
    public class EffectCheckCamera : MonoBehaviour
    {
        [SerializeField] Transform target;

        [SerializeField] float distance = 4.0f;
        [SerializeField] float minDistance = 1.0f;
        [SerializeField] float maxDistance = 20.0f;

        [Tooltip("ドラッグ 1 ピクセルあたりの回転（度）")]
        [SerializeField] float rotateSpeed = 0.3f;
        [Tooltip("ホイール 1 目盛りあたりの距離の変化の割合")]
        [SerializeField] float zoomRate = 0.1f;

        [SerializeField] float yaw = 150.0f;
        [SerializeField] float pitch = 15.0f;

        /// <summary>回り込む目標を切り替える（テスターが複数あるとき、見るテスターを選ぶ）</summary>
        public void SetTarget(Transform target, float distance)
        {
            this.target = target;
            this.distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    var delta = mouse.delta.ReadValue();
                    yaw += delta.x * rotateSpeed;
                    pitch = Mathf.Clamp(pitch - delta.y * rotateSpeed, -80.0f, 80.0f);
                }

                // UI のスクロール（パーツの一覧）と取り合わないよう、UI の上ではズームしない
                var overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                var scroll = mouse.scroll.ReadValue().y;
                if (!overUi && Mathf.Abs(scroll) > 0.0f)
                    distance = Mathf.Clamp(distance * (1.0f - Mathf.Sign(scroll) * zoomRate), minDistance, maxDistance);
            }

            var rotation = Quaternion.Euler(pitch, yaw, 0.0f);
            transform.position = target.position - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
        }
    }
}
