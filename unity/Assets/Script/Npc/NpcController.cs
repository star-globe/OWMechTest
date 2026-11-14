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
                null,       // 砲台（TurretController）と同じく、タグでは絞らない（機体は Untagged）
                out BaseObject target);

            SetLockTarget(found ? target : null);

            if (found && Role == SquadRole.Scout)
                LastKnownTarget = target.transform.position;
        }

        private bool HasLockTarget => LockOn.Target != null;

        // ─── 行動の判断 ──────────────────────────────────────
        // ブースト・ジャンプ・射撃などの判断は INpcDecider に任せる。AI を作り込むときはここを差し替える
        INpcDecider decider;
        public INpcDecider Decider
        {
            get => decider ??= new NpcDecider(ascendHeight);
            set => decider = value;
        }

        // 1 フレームの UpdateInput の中で使う判断の材料（UpdateInput の最初に作り直す）
        NpcDecisionContext context;

        private NpcDecisionContext CreateContext()
        {
            var param = CharacterParam;
            float? heightToDest = HasAgentPath ? agent.destination.y - transform.position.y : (float?)null;
            return new NpcDecisionContext(IsBoost,
                                          param?.Energy ?? 0,
                                          param?.MaxEnergy ?? 0,
                                          IsFloating,
                                          HasLockTarget,
                                          heightToDest);
        }

        protected override bool CheckLeftFire()      => Decider.ShouldFire(WeaponSlot.Left, context);
        protected override bool CheckRightFire()     => Decider.ShouldFire(WeaponSlot.Right, context);
        protected override bool CheckBoost()         => Decider.ShouldToggleBoost(context);
        protected override bool CheckJump()          => Decider.ShouldJump(context);
        protected override bool CheckQuick()         => Decider.ShouldQuick(context);
        protected override bool CheckHyperBoost()    => Decider.ShouldHyperBoost(context);
        protected override bool CheckAscend()        => Decider.ShouldAscend(context);

        // ─── 経路に沿った移動 ─────────────────────────────────
        // NavMeshAgent は経路を求めるだけにして（位置・回転は動かさない）、移動は機体と同じ Rigidbody の処理で行う

        [SerializeField]
        [Tooltip("この角度（度）以上ずれていたら、最大の速さで旋回する")]
        float turnFullAngle = 45f;

        private bool HasAgentPath => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && agent.hasPath;

        /// <summary>経路の次の曲がり角への向きを、機体から見た移動の入力にする</summary>
        protected override void UpdateXZ(out float inputX, out float inputZ)
        {
            inputX = 0;
            inputZ = 0;
            if (!HasAgentPath)
                return;

            var toCorner = agent.steeringTarget - transform.position;
            toCorner.y = 0;
            if (toCorner.sqrMagnitude < StopDistance * StopDistance)
                return;

            var local = transform.InverseTransformDirection(toCorner.normalized);
            inputX = local.x;
            inputZ = local.z;
        }

        const float StopDistance = 0.5f;

        /// <summary>ロックしている目標、なければ経路の次の曲がり角の方へ旋回する</summary>
        protected override Vector2 GetCameraPos()
        {
            Vector3? lookPoint = HasLockTarget ? LockOn.Target.transform.position
                               : HasAgentPath ? agent.steeringTarget
                               : (Vector3?)null;
            if (!lookPoint.HasValue)
                return Vector2.zero;

            var dir = lookPoint.Value - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude < StopDistance * StopDistance)
                return Vector2.zero;

            var angle = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
            return new Vector2(CalcTurnInput(angle, turnFullAngle), 0);
        }

        /// <summary>向きのずれ（度。右が正）から旋回の入力（-1〜1）を求める。turnFullAngle 以上ずれていれば最大</summary>
        public static float CalcTurnInput(float signedAngle, float turnFullAngle)
        {
            if (turnFullAngle <= 0)
                return signedAngle > 0 ? 1f : signedAngle < 0 ? -1f : 0f;

            return Mathf.Clamp(signedAngle / turnFullAngle, -1f, 1f);
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
            SyncAgentPosition();
            UpdateLockTarget();
            UpdateAgentDestination();
            context = CreateContext();
            base.UpdateInput();
        }

        /// <summary>
        /// 経路の計算の起点を機体の位置に合わせる。Agent には位置・回転を動かさせない。
        /// （基底の Start が private のため、派生クラスで Start を定義せずにここで設定する）
        /// </summary>
        private void SyncAgentPosition()
        {
            if (agent == null || !agent.isActiveAndEnabled)
                return;

            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.nextPosition = transform.position;
        }

        private void UpdateAgentDestination()
        {
            // NavMesh の上にいないとき（スポーン直後の落下中など）は経路を設定できない
            if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

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
