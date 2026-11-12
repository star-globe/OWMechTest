using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニットの被弾。HP はユニットの MaxAp から始め、受けたダメージを UnitParam にも反映する
    /// （AP が 0 になると BaseUnit.IsAlive が false になり、移動と射撃が止まる）
    /// </summary>
    public class UnitHitReceiver : HitReceiver
    {
        [SerializeField]
        BaseUnit unit;

        private void Start()
        {
            if (unit == null)
                TryGetComponent(out unit);

            // FixedUnitSpawner は Instantiate の直後に Initialize するため、Start の時点で MaxAp が入っている
            if (unit != null && unit.MaxAp > 0)
                SetInitHitPoint(unit.MaxAp);
        }

        protected override void OnDamaged(int damage)
        {
            if (unit != null)
                unit.SetDamage(damage);
        }
    }
}
