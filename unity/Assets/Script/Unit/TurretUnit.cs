using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 固定砲台ユニット
    /// 移動なし・砲塔回転（X/Y軸）・仰俯角制限付き射撃
    /// </summary>
    public class TurretUnit : BaseUnit
    {
        public override UnitType UnitType => UnitType.Turret;
    }
}
