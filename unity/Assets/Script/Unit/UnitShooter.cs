using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニットの射撃。プレイヤーと同じ攻撃ロジックを使う
    /// （LockOnState で一次ロック・二次ロックを管理し、AimSolver で照準方向を求め、弾の初速に自分の速度を加える）。
    /// 索敵は TurretController と同じく、索敵半径内で自分と違う陣営（None を除く）の BaseObject を探す
    /// </summary>
    public class UnitShooter : MonoBehaviour
    {
        // ── SerializeField ───────────────────────────────
        [SerializeField]
        BaseUnit unit;

        /// <summary>弾を撃つ位置。未設定ならユニットの AimPosition</summary>
        [SerializeField]
        Transform muzzle;

        /// <summary>照準方向へ水平に回す部分（砲塔など。見た目だけ。未設定なら回さない）</summary>
        [SerializeField]
        Transform turret;

        // 索敵結果バッファ（GC 抑制用）
        private static readonly BaseObject[] searchBuffer = new BaseObject[32];

        readonly LockOnState lockOn = new LockOnState();

        /// <summary>ロックの状態</summary>
        public LockOnState LockOn => lockOn;

        // ── Unity Lifecycle ──────────────────────────────
        private void Start()
        {
            if (unit == null)
                TryGetComponent(out unit);
        }

        private void Update()
        {
            if (unit == null || unit.IsAlive == false)
            {
                lockOn.Clear();
                return;
            }

            var param = unit.UnitParam;
            if (param == null)
                return;

            var muzzlePos = muzzle != null ? muzzle.position : unit.AimPosition;

            lockOn.SetTarget(SearchTarget(muzzlePos, param));
            lockOn.AddTime(Time.deltaTime);

            var target = lockOn.Target;
            if (target == null)
                return;

            var fallbackDir = muzzle != null ? muzzle.forward : transform.forward;
            var shooterVelocity = unit.Velocity;
            var request = new AimRequest(target, lockOn.GetLevel(param.SecondaryLockTime), target.AimPosition, shooterVelocity);
            var dir = AimSolver.CalcAimDirection(request, muzzlePos, GetBulletSpeed(param.BulletId), fallbackDir);

            RotateTurret(dir);

            if (param.CanShoot == false || param.CanFire == false)
                return;

            BulletManager.Instance.Fire(param.BulletId, muzzlePos, dir, shooterVelocity);
            param.ResetFireTimer();
        }

        // ── 索敵 ─────────────────────────────────────────
        /// <summary>目標を切り替える距離の比。今の目標の距離のこの割合より近い目標が現れたら切り替える</summary>
        const float SwitchDistanceRate = 0.8f;

        /// <summary>
        /// 範囲内で最も近い目標を返す（いなければ null）。
        /// ただし今ロックしている目標が範囲内にいて、それより明らかに近い目標がいなければ、今の目標を狙い続ける
        /// （距離がほぼ同じ目標の間でロックが切り替わり続け、二次ロックの時間が途切れるのを防ぐ）
        /// </summary>
        BaseObject SearchTarget(Vector3 pos, UnitParam param)
        {
            var count = PhysicsUtils.OverlapShpereOthers(
                pos,
                param.SearchRadius / GlobalParamMaster.Instance.WorldSizeRate,
                unit.Side.ToNpcExcludeMask(),         // 自陣営と None は狙わない
                GameLayers.EnemyLayerMask,
                null,
                searchBuffer);

            var current = lockOn.Target;
            float currentSqrDist = float.MaxValue;
            BaseObject nearest = null;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var obj = searchBuffer[i];
                searchBuffer[i] = null;
                if (obj == null || obj == unit) continue;

                var sqrDist = (obj.transform.position - pos).sqrMagnitude;
                if (obj == current)
                    currentSqrDist = sqrDist;

                if (sqrDist >= minSqrDist) continue;

                minSqrDist = sqrDist;
                nearest = obj;
            }

            if (current != null && currentSqrDist < float.MaxValue
                && minSqrDist >= currentSqrDist * SwitchDistanceRate * SwitchDistanceRate)
                return current;

            return nearest;
        }

        static float GetBulletSpeed(int bulletId)
        {
            var bullet = BulletMaster.Instance.GetSettings(bulletId);
            return bullet == null ? 0f : bullet.Speed;
        }

        void RotateTurret(Vector3 dir)
        {
            if (turret == null) return;

            var flat = new Vector3(dir.x, 0f, dir.z);
            if (flat.sqrMagnitude < 1e-6f) return;

            turret.rotation = Quaternion.LookRotation(flat, Vector3.up);
        }
    }
}
