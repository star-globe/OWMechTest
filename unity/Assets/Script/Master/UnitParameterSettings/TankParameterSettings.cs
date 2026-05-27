using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 戦車固有パラメーター設定
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Unit/TankParameterSettings", order = 2)]
    public class TankParameterSettings : UnitParameterSettings
    {
        [Header("砲塔回転速度")]
        /// <summary>X軸（俯仰）回転速度（度/秒）</summary>
        [SerializeField]
        float turretRotationSpeedX = 20.0f;
        public float TurretRotationSpeedX => turretRotationSpeedX;

        /// <summary>Y軸（水平）回転速度（度/秒）</summary>
        [SerializeField]
        float turretRotationSpeedY = 30.0f;
        public float TurretRotationSpeedY => turretRotationSpeedY;

        [Header("仰俯角制限（度）")]
        /// <summary>最大俯角（下向き、負値）</summary>
        [SerializeField]
        float elevationMin = -5.0f;
        public float ElevationMin => elevationMin;

        /// <summary>最大迎角（上向き）</summary>
        [SerializeField]
        float elevationMax = 20.0f;
        public float ElevationMax => elevationMax;

        [Header("車体移動")]
        /// <summary>直進速度（m/s）</summary>
        [SerializeField]
        float forwardSpeed = 5.0f;
        public float ForwardSpeed => forwardSpeed;

        /// <summary>旋回速度・Y軸（度/秒）</summary>
        [SerializeField]
        float turnSpeed = 60.0f;
        public float TurnSpeed => turnSpeed;
    }
}
