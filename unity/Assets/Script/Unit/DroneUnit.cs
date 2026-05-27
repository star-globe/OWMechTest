using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ドローンユニット
    /// 空中移動タイプ（水平・垂直移動速度）・回転速度・射撃
    /// </summary>
    public class DroneUnit : BaseUnit
    {
        public override UnitType UnitType => UnitType.Drone;
    }
}
