using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 戦車ユニット
    /// 地上移動タイプ（直進速度・旋回速度）・砲塔回転（X/Y軸）・仰俯角制限付き射撃
    /// </summary>
    public class TankUnit : BaseUnit
    {
        public override UnitType UnitType => UnitType.Tank;
    }
}
