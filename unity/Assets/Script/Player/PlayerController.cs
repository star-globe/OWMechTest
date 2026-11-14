using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>
    /// 機体の操作。入力（virtual メソッド。MyPlayerController / NpcController が差し替える）・Rigidbody・TouchChecker と、
    /// 移動の計算（PlayerMoveCalculator）・エフェクト（PlayerEffectPresenter）をつなぐ
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        PlayerCharacter playerCharacter = null;

        [SerializeField]
        Rigidbody rigid = null;

        [SerializeField]
        TouchChecker touchChecker = null;

        [SerializeField]
        PlayerControllerSettings controllerSettings;

        [SerializeField]
        PlayerPartsContainer partsContainer = null;

        PlayerEffectPresenter effectPresenter;

        PlayerMoveParams MoveParams => PlayerMoveParams.From(controllerSettings, PlayerControllerConst.Instance);

        protected CharacterParam CharacterParam => playerCharacter.CharacterParam;

        protected bool IsBoost
        {
            get
            {
                return playerCharacter.IsBoost;
            }
            set
            {
                playerCharacter.IsBoost = value;
            }
        }

        JumpInfo jumpInfo = new JumpInfo();
        JumpInfo quickInfo = new JumpInfo();
        HyperBoostInfo hypBstInfo = new HyperBoostInfo();

        public UnitSide UnitSide
        {
            get
            {
                return playerCharacter == null ? UnitSide.None: playerCharacter.Side;
            }
        }

        public float PlayerHeight
        {
            get
            {
                return playerCharacter == null ? 0: playerCharacter.PlayerHeight;
            }
        }

        float inputX = 0;
        float inputZ = 0;
        bool hasAscendInput = false;

        private void Awake()
        {
            Assert.IsNotNull(controllerSettings);
            Assert.IsNotNull(partsContainer);
            effectPresenter = new PlayerEffectPresenter(partsContainer);
        }

        private void Start()
        {
            ApplyEnergyPerformance();
            SyncParam();
        }

        /// <summary>装備からエネルギーの性能を計算して機体に設定する。ジェネレーターを読み込めなければ機体の設定の値のまま</summary>
        private void ApplyEnergyPerformance()
        {
            if (!partsContainer.TryGetEnergyPerformance(out var performance))
            {
                Debug.LogWarningFormat(this, "[PlayerController] {0}: ジェネレーターを読み込めないため、機体の設定のエネルギー性能を使います", name);
                return;
            }

            if (!performance.IsValid)
            {
                Debug.LogWarningFormat(this, "[PlayerController] {0}: 回復力が 0 以下の組み合わせです（OFF: {1} / ON: {2}）",
                                       name, performance.RecoveryBoostOff, performance.RecoveryBoostOn);
            }

            playerCharacter.CharacterParam.SetEnergyPerformance(performance);
        }

        private void SyncParam()
        {
            this.IsBoost = playerCharacter.IsBoost;
        }

        /// <summary>
        /// 撃破されたときに操作を止める。入力・移動・射撃をやめ、ブーストを切って速度を 0 にする。
        /// NPC は AI の移動と部隊も止める（NpcController）
        /// </summary>
        public virtual void StopControl()
        {
            hypBstInfo.ResetBoost();
            playerCharacter.IsHyperBoost = false;
            this.IsBoost = false;
            LockOn.Clear();

            inputX = 0;
            inputZ = 0;
            hasAscendInput = false;

            if (rigid != null && !rigid.isKinematic)
            {
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
            }

            this.enabled = false;
        }

        private void Update()
        {
            LockOn.AddTime(Time.deltaTime);
            UpdateInput();
            UpdateParam();
            UpdateEffect();
        }

        void FixedUpdate()
        {
            UpdateMove();
            UpdateRotae();
        }

        #region 入力（派生クラスで差し替える）

        protected virtual void UpdateXZ(out float inputX, out float inputZ)
        {
            inputX = 0;
            inputZ = 0;
        }
        protected virtual Vector2 GetCameraPos()
        {
            return Vector2.zero;
        }

        // CheckJump / CheckQuick / CheckHyperBoost の基底は NPC 向けの判断で、消費量より多く残っているときだけ行動する
        // （オーバーヒートしない範囲で行動させる）。プレイヤーは残量が 0 以上なら行動できる（CharacterParam.CanJump など）

        protected virtual bool CheckJump()
        {
            var param = playerCharacter.CharacterParam;
            return param.HasEnergyFor(param.JumpEnergy);
        }
        /// <summary>
        /// グライド中の上昇の入力（押し続けている間 true）。エネルギーや空中かどうかは PlayerController 側で判定する。
        /// CheckJump と違い、基底では false を返す（エネルギーだけで判定すると NPC が空中で常に上昇し続けるため）
        /// </summary>
        protected virtual bool CheckAscend()
        {
            return false;
        }
        /// <summary>
        /// ブーストの ON / OFF を切り替えるか。切り替えはエネルギーの残量に左右されない。
        /// CheckAscend と同じく基底では false を返す（派生クラスが入力や AI で判定する）
        /// </summary>
        protected virtual bool CheckBoost()
        {
            return false;
        }
        protected virtual bool CheckQuick()
        {
            var param = playerCharacter.CharacterParam;
            return param.HasEnergyFor(param.QuickBoostEnergy);
        }
        protected virtual bool CheckHyperBoost()
        {
            var param = playerCharacter.CharacterParam;
            return param.HasEnergyFor(param.HyperBoostEnergy);
        }
        protected virtual bool CheckRightFire()
        {
            return false;
        }
        protected virtual bool CheckLeftFire()
        {
            return false;
        }
        protected virtual bool CheckSubFire()
        {
            return false;
        }

        /// <summary>ロックしていないときに狙う点（既定は正面）</summary>
        protected virtual Vector3 GetFallbackAimPoint()
        {
            var trans = this.transform;
            return trans.position + trans.forward * DefaultFallbackLength + Vector3.up * this.PlayerHeight;
        }

        const float DefaultFallbackLength = 100f;

        #endregion

        #region ロック

        /// <summary>ロックの状態。プレイヤーはロック UI（LockSiteDisplay）が、NPC は索敵が目標を設定する</summary>
        public LockOnState LockOn { get; } = new LockOnState();

        /// <summary>ロックの目標を設定する（null でロックを外す）。今と違う目標なら二次ロックまでの時間を数え直す</summary>
        public void SetLockTarget(BaseObject target)
        {
            LockOn.SetTarget(target);
        }

        /// <summary>その武器での今のロックの段階</summary>
        public LockLevel GetLockLevel(WeaponSlot slot)
        {
            // 目標が破棄されていたらロックを外す
            if (LockOn.HasTarget && LockOn.Target == null)
                LockOn.Clear();

            return LockOn.GetLevel(partsContainer.GetSecondaryLockTime(slot));
        }

        /// <summary>自機の速度（弾が引き継ぐ）</summary>
        public Vector3 Velocity => rigid != null ? rigid.linearVelocity : Vector3.zero;

        #endregion

        protected virtual void UpdateInput()
        {
            UpdateXZ(out inputX, out inputZ);

            var p = MoveParams;

            if (CheckJump())
            {
                if (jumpInfo.isJump == false && jumpInfo.interval <= 0 && isTouched)
                {
                    StartJump(p);
                }
                else
                {
                    Debug.LogFormat("IsJump:{0} IsTouched:{1}", jumpInfo.isJump, isTouched);
                }
            }
            hasAscendInput = CheckAscend();
            if (CheckBoost())
            {
                this.IsBoost = !this.IsBoost;
            }
            if (CheckQuick())
            {
                if (quickInfo.isJump == false && quickInfo.interval <= 0)
                {
                    StartQuick(p);
                }
                else
                {
                    Debug.LogFormat("IsQuick:{0} IsTouched:{1}", quickInfo.isJump, isTouched);
                }
            }
            if (hypBstInfo.isOn)
            {
                if (PlayerMoveCalculator.ShouldCancelHyperBoost(jumpInfo.isJump, inputX, inputZ, hypBstInfo.inputVector))
                {
                    hypBstInfo.ResetBoost();
                    playerCharacter.IsHyperBoost = false;
                }
            }
            else if (CheckHyperBoost() && IsBoost)
            {
                hypBstInfo.SetBoost(PlayerMoveCalculator.CalcHyperBoostInput(inputX, inputZ), p.HyperBoostSpeed);
                playerCharacter.IsHyperBoost = true;
            }
            if (CheckRightFire())
            {
                Fire(WeaponSlot.Right);
            }

            if (CheckLeftFire())
            {
                Fire(WeaponSlot.Left);
            }

            if (CheckSubFire())
            {
                Fire(WeaponSlot.Sub);
            }
        }

        private void StartJump(in PlayerMoveParams p)
        {
            PlayerMoveCalculator.CalcJumpDirection(inputX, inputZ, out var horizontal, out var local);
            if (PlayerMoveCalculator.HasInput(inputX, inputZ))
                horizontal = rigid.transform.TransformDirection(horizontal);

            var jumpVec = touchChecker == null ? Vector3.zero :
                PlayerMoveCalculator.KickAndJump(p, touchChecker.CurrentTouchState, touchChecker.CurrentNormal, horizontal);

            jumpInfo.SetJump(jumpVec, local, p.JumpInterval);
            playerCharacter.IgnitJumpBoost();
        }

        private void StartQuick(in PlayerMoveParams p)
        {
            var localVec = PlayerMoveCalculator.CalcQuickLocalVector(p, inputX, inputZ, isFloating);
            var quickVec = rigid.transform.TransformDirection(localVec);

            // 接触中に TouchQuickRate を掛ける処理は、分割前のコードでも計算結果が使われていなかった（#64）
            quickInfo.SetJump(quickVec * p.QuickSpeed, localVec, p.QuickInterval);
            playerCharacter.IgnitQuickBoost();
        }

        void UpdateParam()
        {
            var time = Time.deltaTime;
            playerCharacter.IsAscend = isAscend;
            playerCharacter.UpdateParam(time, isFloating);

            // オーバーヒートしたら、継続して消費する行動を止める。グライド中の上昇は CanAscend が false になるので止まる。
            // 原因になったジャンプ・クイックブーストは最後まで出す
            if (playerCharacter.CharacterParam.IsOverheat && hypBstInfo.isOn)
            {
                hypBstInfo.ResetBoost();
                playerCharacter.IsHyperBoost = false;
            }
        }

        protected virtual void Fire(WeaponSlot slot)
        {
            var request = new AimRequest(LockOn.Target, GetLockLevel(slot), GetFallbackAimPoint(), Velocity, playerCharacter);
            partsContainer.Fire(slot, request);
        }

        private void UpdateMove()
        {
            var p = MoveParams;
            var state = CurrentMoveState;
            var delta = Time.deltaTime;

            bool hasInput = PlayerMoveCalculator.HasInput(inputX, inputZ);
            Vector3 inputDir = Vector3.zero;
            if (hasInput)
            {
                inputDir = new Vector3(inputX, 0, inputZ);
                inputDir.Normalize();
                inputDir = rigid.transform.TransformDirection(inputDir);
            }

            var hyperBoostVec = state.IsHyperBoost ? rigid.transform.TransformDirection(hypBstInfo.localBoostVector) : Vector3.zero;
            var vel = rigid.linearVelocity;

            var vec = PlayerMoveCalculator.CalcAccel(p, state, hasInput, inputDir, vel, jumpInfo.isJump, hyperBoostVec, delta);

            vec += PlayerMoveCalculator.StepImpulse(ref jumpInfo, p.TouchTime, p.JumpTime, state.IsTouched, delta, out var jumpEnded);
            if (jumpEnded)
                playerCharacter.QuitJumpBoost();

            vec += PlayerMoveCalculator.StepImpulse(ref quickInfo, p.QuickTime, p.QuickTime, state.IsTouched, delta, out var quickEnded);
            if (quickEnded)
                playerCharacter.QuitQuickBoost();

            if (PlayerMoveCalculator.ShouldBrake(state.IsBoost, hasInput, jumpInfo.isJump, quickInfo.isJump))
            {
                vec = PlayerMoveCalculator.CalcBrake(p, vel, state.IsTouched, delta);
            }

            if (isAscend)
            {
                vec += PlayerMoveCalculator.CalcAscendForce(p, vel.y, Physics.gravity.y, delta);
            }
            else if (PlayerMoveCalculator.ShouldFloat(state.IsFloating, state.IsBoost, vel.y))
            {
                vec += PlayerMoveCalculator.CalcFloatForce(p, Physics.gravity.y, delta);
            }

            rigid.AddForce(vec, ForceMode.VelocityChange);

            var velocity = rigid.linearVelocity;
            playerCharacter.SetSpeed(velocity.magnitude);
            partsContainer.SetMove(new Vector2(velocity.x, velocity.z).magnitude, !state.IsFloating);
        }

        private void UpdateEffect()
        {
            effectPresenter.Apply(IsBoost, isFloating, isAscend, inputX, inputZ, jumpInfo, quickInfo, hypBstInfo);
        }

        private PlayerMoveState CurrentMoveState
        {
            get
            {
                return new PlayerMoveState
                {
                    IsBoost = this.IsBoost,
                    IsHyperBoost = hypBstInfo.isOn,
                    IsFloating = isFloating,
                    IsTouched = isTouched,
                    IsWallTouched = isWallTouched,
                    TouchNormal = touchChecker != null ? touchChecker.CurrentNormal : Vector3.zero,
                };
            }
        }

        private bool isFloating
        {
            get
            {
                return touchChecker != null &&
                       touchChecker.CurrentTouchState != TouchState.Ground;
            }
        }

        /// <summary>グライド中の上昇をしているか</summary>
        private bool isAscend
        {
            get
            {
                return PlayerMoveCalculator.ShouldAscend(hasAscendInput, isFloating, IsBoost,
                                                         playerCharacter.CharacterParam.CanAscend);
            }
        }

        private bool isTouched
        {
            get
            {
                return touchChecker != null &&
                       touchChecker.CurrentTouchState != TouchState.None;
            }
        }

        private bool isWallTouched
        {
            get
            {
                return touchChecker != null &&
                       touchChecker.CurrentTouchState == TouchState.Wall;
            }
        }

        private void UpdateRotae()
        {
            var cameraVec2 = GetCameraPos();
            if (PlayerMoveCalculator.TryCalcRotateDegree(MoveParams, cameraVec2.x, Time.deltaTime, out var rotDeg))
            {
                rigid.transform.Rotate(Vector3.up * rotDeg);
            }
        }
    }
}
