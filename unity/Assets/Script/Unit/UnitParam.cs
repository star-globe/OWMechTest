using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニットの実行時パラメーター状態を管理するクラス（CharacterParam の Unit 版）
    /// </summary>
    public class UnitParam
    {
        // ── HP ──────────────────────────────────────────
        public int Ap { get; private set; }
        public int MaxAp { get; private set; }

        // ── 射撃共通 ────────────────────────────────────
        public int BulletId { get; private set; }
        public float FireInterval { get; private set; }
        private float fireTimer;

        // ── 砲塔回転（固定砲台・戦車共通） ─────────────
        public float TurretRotationSpeedX { get; private set; }
        public float TurretRotationSpeedY { get; private set; }
        public float ElevationMin { get; private set; }
        public float ElevationMax { get; private set; }

        // ── 索敵（固定砲台） ─────────────────────────────
        public float SearchRadius { get; private set; }

        // ── 移動（ドローン） ────────────────────────────
        public float HorizontalMoveSpeed { get; private set; }
        public float VerticalMoveSpeed { get; private set; }
        public float RotationSpeed { get; private set; }

        // ── 移動（戦車） ────────────────────────────────
        public float ForwardSpeed { get; private set; }
        public float TurnSpeed { get; private set; }

        // ── 初期化 ──────────────────────────────────────
        public void SetInitialInfo(UnitParameterSettings param)
        {
            Ap    = param.MaxAp;
            MaxAp = param.MaxAp;
            BulletId     = param.BulletId;
            FireInterval = param.FireInterval;
            fireTimer    = 0f;

            if (param is TurretParameterSettings turret)
            {
                TurretRotationSpeedX = turret.TurretRotationSpeedX;
                TurretRotationSpeedY = turret.TurretRotationSpeedY;
                ElevationMin         = turret.ElevationMin;
                ElevationMax         = turret.ElevationMax;
                SearchRadius         = turret.SearchRadius;
            }
            else if (param is DroneParameterSettings drone)
            {
                HorizontalMoveSpeed = drone.HorizontalMoveSpeed;
                VerticalMoveSpeed   = drone.VerticalMoveSpeed;
                RotationSpeed       = drone.RotationSpeed;
            }
            else if (param is TankParameterSettings tank)
            {
                TurretRotationSpeedX = tank.TurretRotationSpeedX;
                TurretRotationSpeedY = tank.TurretRotationSpeedY;
                ElevationMin         = tank.ElevationMin;
                ElevationMax         = tank.ElevationMax;
                ForwardSpeed         = tank.ForwardSpeed;
                TurnSpeed            = tank.TurnSpeed;
            }
        }

        // ── ダメージ ────────────────────────────────────
        public void SetDamage(int damage)
        {
            Ap = Mathf.Max(Ap - damage, 0);
        }

        public bool IsAlive => Ap > 0;

        // ── 射撃タイマー ────────────────────────────────
        public bool CanFire => fireTimer <= 0f;

        public void UpdateFireTimer(float deltaTime)
        {
            if (fireTimer > 0f)
                fireTimer -= deltaTime;
        }

        public void ResetFireTimer()
        {
            fireTimer = FireInterval;
        }
    }
}
