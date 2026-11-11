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

        bool IsBoost
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

        private void Awake()
        {
            Assert.IsNotNull(controllerSettings);
            Assert.IsNotNull(partsContainer);
            effectPresenter = new PlayerEffectPresenter(partsContainer);
        }

        private void Start()
        {
            SyncParam();
        }

        private void SyncParam()
        {
            this.IsBoost = playerCharacter.IsBoost;
        }

        private void Update()
        {
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

        protected virtual bool CheckJump()
        {
            return playerCharacter.CharacterParam.CanJump;
        }
        protected virtual bool CheckBoost()
        {
            return playerCharacter.CharacterParam.CanBoost;
        }
        protected virtual bool CheckQuick()
        {
            return playerCharacter.CharacterParam.CanQuick;
        }
        protected virtual bool CheckHyperBoost()
        {
            return playerCharacter.CharacterParam.CanHyperBoost;
        }
        protected virtual bool CheckRightFire(out Vector3 target)
        {
            target = Vector3.zero;
            return false;
        }
        protected virtual bool CheckLeftFire(out Vector3 target)
        {
            target = Vector3.zero;
            return false;
        }
        protected virtual bool CheckSubFire(out Vector3 target)
        {
            target = Vector3.zero;
            return false;
        }

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
            Vector3 tgt;
            if (CheckRightFire(out tgt))
            {
                Fire(tgt, isRight: true);
            }

            if (CheckLeftFire(out tgt))
            {
                Fire(tgt, isRight: false);
            }

            if (CheckSubFire(out tgt))
            {
                SubFire(tgt);
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
            playerCharacter.UpdateParam(time, isFloating);
        }

        protected virtual void Fire(Vector3 tgt, bool isRight)
        {
            if (isRight)
            {
                partsContainer.RightFire(tgt);
            }
            else
            {
                partsContainer.LeftFire(tgt);
            }
        }

        protected virtual void SubFire(Vector3 tgt)
        {
            partsContainer.SubFire(tgt);
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

            if (PlayerMoveCalculator.ShouldFloat(state.IsFloating, state.IsBoost, vel.y))
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
            effectPresenter.Apply(IsBoost, isFloating, inputX, inputZ, jumpInfo, quickInfo, hypBstInfo);
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
