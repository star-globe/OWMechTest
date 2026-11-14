using UnityEngine;
using UnityEngine.AI;

namespace AdvancedGears
{
    /// <summary>
    /// 移動可能NPCのコントローラー。諸兵科連合（ISquadMember）に参加できる。
    ///
    /// 探知距離は個々のユニット設定値（radius）に依存する。
    /// 移動目的地が設定されている場合の実効探知距離：
    ///   effectiveRadius = radius - Max(0, distToDestination - goalDistance)
    /// → 目的地に近づくほど探知距離が広がり、敵への攻撃に移行しやすくなる。
    /// </summary>
    public class NpcController : PlayerController, ISquadMember
    {
        // ─── 探知・移動パラメーター ───────────────────────────
        [SerializeField]
        float radius = 100f;
        public float DefaultRadius => radius;

        [SerializeField]
        NavMeshAgent agent;

        [SerializeField]
        [Tooltip("目的地がこの高さ以上、上にあるときにグライド中の上昇をする")]
        float ascendHeight = 5f;

        // ─── ISquadMember ────────────────────────────────────
        public SquadRole Role { get; private set; } = SquadRole.None;
        public SquadData Squad { get; private set; }

        private bool hasDestination;
        private Vector3 destination;
        private float goalDistance;

        public void SetSquad(SquadData squad, SquadRole role)
        {
            Squad?.RemoveMember(this);
            Squad = squad;
            Role  = role;
            squad.AddMember(this);
        }

        public void SetDestination(Vector3 dest, float goalDist)
        {
            hasDestination = true;
            destination    = dest;
            goalDistance   = goalDist;

            // リーダーは受け取った目的地をさらに下位メンバーへ分配する
            if (Role == SquadRole.Leader && Squad != null)
                DistributeDestinationToMembers(dest, goalDist);
        }

        public void ClearDestination()
        {
            hasDestination = false;
        }

        // 偵察ユニットが最後に発見した目標（SquadManager が収集する）
        public Vector3? LastKnownTarget { get; private set; }

        // ─── 実効探知距離 ─────────────────────────────────────
        private float EffectiveRadius
        {
            get
            {
                if (!hasDestination) return radius;
                float distToDest = Vector3.Distance(transform.position, destination);
                float remaining  = Mathf.Max(0f, distToDest - goalDistance);
                return Mathf.Clamp(radius - remaining, 0f, radius);
            }
        }

        // ─── 目標探索とロック ─────────────────────────────────
        // 毎フレーム最近傍の目標を探してロックする。見つけた時点で一次ロック、
        // 同じ目標を狙い続けて銃ごとの時間が経つと二次ロック（偏差射撃）になる
        private void UpdateLockTarget()
        {
            var pos = this.transform.position;
            bool found = PhysicsUtils.CheckOverlapShpereOthers(
                pos,
                EffectiveRadius,
                this.UnitSide.ToNpcExcludeMask(),
                GameLayers.EnemyLayerMask,
                PhysicsUtils.PlayerTag,
                out BaseObject target);

            SetLockTarget(found ? target : null);

            if (found && Role == SquadRole.Scout)
                LastKnownTarget = target.transform.position;
        }

        private bool HasLockTarget => LockOn.Target != null;

        // ─── 射撃判定 ─────────────────────────────────────────
        protected override bool CheckLeftFire()
        {
            return HasLockTarget;
        }

        protected override bool CheckRightFire()
        {
            return HasLockTarget;
        }

        // ─── ブースト ────────────────────────────────────────
        // 暫定：残量の割合で切り替える（NpcBoostDecider）
        protected override bool CheckBoost()
        {
            var param = CharacterParam;
            return param != null && NpcBoostDecider.ShouldToggle(IsBoost, param.Energy, param.MaxEnergy);
        }

        // ─── 上昇 ────────────────────────────────────────────
        // 最低限の判定：経路の目的地が自分より ascendHeight 以上高いときだけ上昇する（空中・ブースト中かどうかは基底で判定）
        protected override bool CheckAscend()
        {
            if (agent == null || !agent.isActiveAndEnabled || !agent.hasPath) return false;
            return agent.destination.y - transform.position.y >= ascendHeight;
        }

        // ─── 撃破 ────────────────────────────────────────────
        public override void StopControl()
        {
            base.StopControl();

            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                agent.ResetPath();
            if (agent != null)
                agent.enabled = false;

            // 部隊から外す（撃破された機体はリーダーの目的地の配布や部隊の中心の計算に含めない）
            Squad?.RemoveMember(this);
            Squad = null;
            Role  = SquadRole.None;
            hasDestination = false;
        }

        // ─── 移動AI ──────────────────────────────────────────
        protected override void UpdateInput()
        {
            UpdateLockTarget();
            UpdateAgentDestination();
            base.UpdateInput();
        }

        private void UpdateAgentDestination()
        {
            if (agent == null) return;

            var order = Squad?.CurrentOrder ?? SquadOrder.Advance;

            switch (order)
            {
                case SquadOrder.Idle:
                case SquadOrder.Defend:
                    agent.ResetPath();
                    return;

                case SquadOrder.Retreat:
                    agent.SetDestination(GetRetreatPoint());
                    return;
            }

            // Advance：移動目的地があればそこへ、なければ自律探索
            if (hasDestination)
            {
                float dist = Vector3.Distance(transform.position, destination);
                if (dist > goalDistance)
                    agent.SetDestination(destination);
                else
                    agent.ResetPath();
            }
            else if (HasLockTarget)
            {
                agent.SetDestination(LockOn.Target.transform.position);
            }
            else if (Squad?.SharedTarget.HasValue == true)
            {
                agent.SetDestination(Squad.SharedTarget.Value);
            }
        }

        private Vector3 GetRetreatPoint()
        {
            if (Squad == null) return transform.position;
            var center = Squad.GetCenterPosition();
            var awayDir = (transform.position - center).normalized;
            return transform.position + awayDir * 50f;
        }

        // ─── リーダーによる目的地配布 ─────────────────────────
        private void DistributeDestinationToMembers(Vector3 dest, float goalDist)
        {
            foreach (var member in Squad.Members)
            {
                if (member == (ISquadMember)this) continue;
                if (member.Role == SquadRole.Leader)  continue; // 下位リーダーは自分で管理

                // 役割別にオフセットを加えて目的地を差別化
                var offset = GetRoleOffset(member.Role, dest);
                member.SetDestination(dest + offset, goalDist);
            }
        }

        private Vector3 GetRoleOffset(SquadRole role, Vector3 dest)
        {
            var toDestDir = (dest - transform.position).normalized;
            return role switch
            {
                SquadRole.Rearguard => -toDestDir * 40f,
                SquadRole.Support   => -toDestDir * 20f,
                SquadRole.Scout     =>  Vector3.Cross(toDestDir, Vector3.up) * 30f,
                _                   =>  Vector3.zero,
            };
        }
    }
}
