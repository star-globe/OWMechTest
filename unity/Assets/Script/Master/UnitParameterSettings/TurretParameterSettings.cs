using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 固定砲台固有パラメーター設定
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Unit/TurretParameterSettings", order = 0)]
    public class TurretParameterSettings : UnitParameterSettings
    {
        [Header("砲塔回転速度")]
        /// <summary>X軸（俯仰）回転速度（度/秒）</summary>
        [SerializeField]
        float turretRotationSpeedX = 30.0f;
        public float TurretRotationSpeedX => turretRotationSpeedX;

        /// <summary>Y軸（水平）回転速度（度/秒）</summary>
        [SerializeField]
        float turretRotationSpeedY = 45.0f;
        public float TurretRotationSpeedY => turretRotationSpeedY;

        [Header("仰俯角制限（度）")]
        /// <summary>最大俯角（下向き、負値）</summary>
        [SerializeField]
        float elevationMin = -10.0f;
        public float ElevationMin => elevationMin;

        /// <summary>最大迎角（上向き）</summary>
        [SerializeField]
        float elevationMax = 45.0f;
        public float ElevationMax => elevationMax;

        [Header("索敵")]
        /// <summary>索敵半径（m）</summary>
        [SerializeField]
        float searchRadius = 50.0f;
        public float SearchRadius => searchRadius;
    }
}
