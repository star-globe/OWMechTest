using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ドローン固有パラメーター設定
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Unit/DroneParameterSettings", order = 1)]
    public class DroneParameterSettings : UnitParameterSettings
    {
        [Header("移動速度")]
        /// <summary>水平移動速度（m/s）</summary>
        [SerializeField]
        float horizontalMoveSpeed = 8.0f;
        public float HorizontalMoveSpeed => horizontalMoveSpeed;

        /// <summary>垂直移動速度（m/s）</summary>
        [SerializeField]
        float verticalMoveSpeed = 4.0f;
        public float VerticalMoveSpeed => verticalMoveSpeed;

        [Header("回転速度")]
        /// <summary>回転速度（度/秒）</summary>
        [SerializeField]
        float rotationSpeed = 120.0f;
        public float RotationSpeed => rotationSpeed;
    }
}
