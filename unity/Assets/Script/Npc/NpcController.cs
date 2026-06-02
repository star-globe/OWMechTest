using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class NpcController : PlayerController
    {
        // ─── 役割・部隊 ──────────────────────────────────────
        public SquadRole Role { get; private set; } = SquadRole.None;
        public SquadData Squad { get; private set; }

        public void JoinSquad(SquadData squad, SquadRole role)
        {
            Squad?.RemoveMember(this);
            Squad = squad;
            Role  = role;
            squad.AddMember(this);
        }

        // 偵察ユニットが最後に発見した目標（SquadManager が参照）
        public Vector3? LastKnownTarget { get; private set; }

        // ─── 検知半径（役割ごとに上書き） ────────────────────
        [SerializeField]
        float baseRadius = 100f;

        private float DetectionRadius
        {
            get
            {
                return Role switch
                {
                    SquadRole.Scout     => baseRadius * 1.8f,   // 偵察は広範囲
                    SquadRole.Rearguard => baseRadius * 1.3f,   // 後衛はやや広め
                    _                   => baseRadius,
                };
            }
        }

        // ─── NavMesh ─────────────────────────────────────────
        [SerializeField]
        NavMeshAgent agent;

        // ─── 目標探索 ─────────────────────────────────────────
        private bool FindTarget(out Vector3 target)
        {
            var pos = this.transform.position;
            bool found = PhysicsUtils.CheckOverlapShpereOthers(
                pos, DetectionRadius,
                this.UnitSide.ToNpcExcludeMask(),
                GameLayers.EnemyLayerMask,
                string.Empty, out target);

            if (found && Role == SquadRole.Scout)
                LastKnownTarget = target;

            return found;
        }

        // ─── 射撃判定（役割別） ───────────────────────────────
        protected override bool CheckRightFire(out Vector3 target)
        {
            return ShouldFire(out target);
        }

        protected override bool CheckLeftFire(out Vector3 target)
        {
            return ShouldFire(out target);
        }

        private bool ShouldFire(out Vector3 target)
        {
            // 支援ユニットは自分では積極的に射撃しない
            if (Role == SquadRole.Support)
            {
                target = Vector3.zero;
                return false;
            }
            return FindTarget(out target);
        }

        // ─── 移動AI ──────────────────────────────────────────
        protected override void UpdateInput()
        {
            UpdateAgentDestination();
            base.UpdateInput();
        }

        private void UpdateAgentDestination()
        {
            if (agent == null) return;

            var order = Squad?.CurrentOrder ?? SquadOrder.Advance;

            switch (order)
            {
                case SquadOrder.Retreat:
                    SetRetreatDestination();
                    break;

                case SquadOrder.Defend:
                    // 現在地をそのまま維持（目的地をリセット）
                    agent.ResetPath();
                    break;

                case SquadOrder.Idle:
                    agent.ResetPath();
                    break;

                default:    // Advance
                    SetAdvanceDestination();
                    break;
            }

            if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
            {
                var corners = new Vector3[16];
                agent.path.GetCornersNonAlloc(corners);
            }
        }

        private void SetAdvanceDestination()
        {
            Vector3? dest = null;

            switch (Role)
            {
                case SquadRole.Rearguard:
                    // 偵察が共有した目標 or 自己探索、ただし一定距離を保つ
                    dest = GetRearguardDestination();
                    break;

                case SquadRole.Support:
                    // 部隊中心に追従
                    if (Squad != null)
                        dest = Squad.GetCenterPosition();
                    break;

                case SquadRole.Scout:
                    // 共有目標があれば接近せずに哨戒、なければ自律探索
                    if (!FindTarget(out var scoutTarget))
                        dest = GetPatrolDestination();
                    else
                        dest = scoutTarget;
                    break;

                default:    // Vanguard / None
                    if (FindTarget(out var t))
                        dest = t;
                    else if (Squad?.SharedTarget.HasValue == true)
                        dest = Squad.SharedTarget.Value;
                    break;
            }

            if (dest.HasValue)
                agent.SetDestination(dest.Value);
        }

        private Vector3? GetRearguardDestination()
        {
            const float keepDistance = 40f;
            Vector3? target = null;

            if (Squad?.SharedTarget.HasValue == true)
                target = Squad.SharedTarget.Value;
            else if (FindTarget(out var t))
                target = t;

            if (!target.HasValue)
                return null;

            var dir = (target.Value - transform.position).normalized;
            return target.Value - dir * keepDistance;
        }

        private Vector3 GetPatrolDestination()
        {
            // 現在地から一定範囲にランダムに哨戒目標を設定
            var random = Random.insideUnitSphere * baseRadius;
            random.y = 0;
            return transform.position + random;
        }

        private void SetRetreatDestination()
        {
            if (Squad == null) return;
            var center = Squad.GetCenterPosition();
            var awayDir = (transform.position - center).normalized;
            agent.SetDestination(transform.position + awayDir * 50f);
        }
    }
}
