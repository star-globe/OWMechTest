using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// UnitSettings の移動パターン（UnitMovePatternSettings）でユニットを動かす。
    /// 位置と速度は IUnitMovePattern の式で求め、Transform を直接動かす。
    /// 地上ユニット（UnitMoveType.Ground）は地面に沿わせ、空中ユニットはパターンの高さのまま動かす
    /// </summary>
    public class UnitMover : MonoBehaviour
    {
        // ── SerializeField ───────────────────────────────
        [SerializeField]
        BaseUnit unit;

        /// <summary>地面を探すレイを、移動先のどれだけ上から撃つか（ワールドの距離）</summary>
        [SerializeField]
        float groundRayHeight = 20f;

        // ── Internal ─────────────────────────────────────
        IUnitMovePattern pattern;
        Vector3 origin;
        Quaternion baseRotation;
        float scale = 1f;
        float elapsed;
        bool followGround;
        float groundOffset;

        /// <summary>移動パターンで動いているか（撃破されたら false）</summary>
        public bool IsMoving => pattern != null && unit != null && unit.IsAlive;

        /// <summary>現在の速度（式で求めた値。止まっているときは 0）</summary>
        public Vector3 Velocity { get; private set; }

        // ── Unity Lifecycle ──────────────────────────────
        private void Start()
        {
            if (unit == null)
                TryGetComponent(out unit);

            // FixedUnitSpawner は Instantiate の直後に Initialize するため、Start の時点で UnitSettings が入っている
            var settings = unit != null ? unit.UnitSettings : null;
            if (settings == null)
                return;

            pattern = UnitMovePatternMaster.CreatePattern(settings.MovePatternId);
            if (pattern == null)
                return;

            origin = transform.position;
            baseRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            scale = 1f / GlobalParamMaster.Instance.WorldSizeRate;
            elapsed = 0f;

            followGround = settings.MoveType == UnitMoveType.Ground;
            if (followGround)
            {
                if (pattern.HasFixedForward)
                    Debug.LogWarningFormat("[UnitMover] 地上ユニットに縦の円運動が設定されています。地面に沿わせます。UnitId:{0}", settings.UnitId);

                groundOffset = TryFindGround(origin, out var groundY) ? origin.y - groundY : 0f;
            }

            Apply(0f);
        }

        private void Update()
        {
            if (IsMoving == false)
            {
                Velocity = Vector3.zero;
                return;
            }

            elapsed += Time.deltaTime;
            Apply(elapsed);
        }

        // ── Move ─────────────────────────────────────────
        void Apply(float time)
        {
            pattern.Evaluate(time, out var localPos, out var localVel);

            var pos = origin + baseRotation * (localPos * scale);
            var vel = baseRotation * (localVel * scale);

            if (followGround && TryFindGround(pos, out var groundY))
                pos.y = groundY + groundOffset;

            transform.position = pos;
            Velocity = vel;

            // 向き：固定（縦の円運動）か、進行方向。止まっているときは今の向きのまま
            var forward = pattern.HasFixedForward
                ? baseRotation * pattern.FixedForward
                : new Vector3(vel.x, 0f, vel.z);

            if (forward.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        bool TryFindGround(Vector3 pos, out float groundY)
        {
            var start = pos + Vector3.up * groundRayHeight;
            if (Physics.Raycast(start, Vector3.down, out var hit, groundRayHeight * 2f, GameLayers.RaycastObstacleMask, QueryTriggerInteraction.Ignore))
            {
                groundY = hit.point.y;
                return true;
            }

            groundY = pos.y;
            return false;
        }
    }
}
