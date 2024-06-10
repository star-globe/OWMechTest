using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public struct JumpInfo
    {
        public bool isJump { get; private set; }
        public float jumpSumTime { get; private set; }
        public Vector3 jumpVector { get; private set; }
        public Vector3 localNor { get; private set; }

        public void SetJump(Vector3 jumpVec, Vector3 local)
        {
            isJump = true;
            jumpSumTime = 0.0f;
            jumpVector = jumpVec;
            this.localNor = local;
        }

        public void UpdateJumpSumTime(float sum)
        {
            jumpSumTime += sum;
        }

        public void ResetJump()
        {
            isJump = false;
            jumpSumTime = 0.0f;
            jumpVector = Vector3.zero;
        }
    }

    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        PlayerCharacter playerCharacter = null;

        [SerializeField]
        Rigidbody rigid = null;

        [SerializeField]
        TouchChecker touchChecker = null;

        #region Paramter
        [SerializeField]
        PlayerControllerSettings controllerSettings;

        float vecSpeed => controllerSettings.VecSpeed;
        float accelerateTime => controllerSettings.AccelerateTime;
        float jumpSpeed => controllerSettings.JumpSpeed;
        float horizontalBoostRate => controllerSettings.HorizontalBoostRate;
        float floatRate => controllerSettings.FloatRate;
        float rotSpeed => controllerSettings.RotSpeed;

        float rotXMin => PlayerControllerConst.Instance.RotXMin;
        float wallJumpRate => PlayerControllerConst.Instance.WallJumpRate;
        float jumpTime => PlayerControllerConst.Instance.JumpTime;
        float touchTime => PlayerControllerConst.Instance.TouchTime;
        float airBrakeRate => PlayerControllerConst.Instance.AirBrakeRate;
        float touchBrakeRateAdd => PlayerControllerConst.Instance.TouchBrakeRateAdd;

        float quickSpeed => controllerSettings.QuickSpeed;
        float touchQuickRate => controllerSettings.TouchQuickRate;
        float quickTime => controllerSettings.QuickTime;
        #endregion

        [SerializeField]
        PlayerPartsContainer partsContainer = null;

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

        bool isJump { get { return jumpInfo.isJump; } }
        float jumpSumTime
        {
            get
            {
                return jumpInfo.jumpSumTime;
            }
            set
            {
                jumpInfo.UpdateJumpSumTime(value);
            }
        }
        Vector3 jumpVector
        {
            get { return jumpInfo.jumpVector; }
        }

        JumpInfo quickInfo = new JumpInfo();

        bool isQuick { get { return quickInfo.isJump; } }
        float quickSumTime
        {
            get
            {
                return quickInfo.jumpSumTime;
            }
            set
            {
                quickInfo.UpdateJumpSumTime(value);
            }
        }
        Vector3 quickVector
        {
            get { return quickInfo.jumpVector; }
        }

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
            UpdateEffect();
        }

        void FixedUpdate()
        {
            UpdateMove();
            UpdateRotae();
        }

        private float LimitSpeed
        {
            get
            {
                if (this.IsBoost)
                    return vecSpeed * horizontalBoostRate;
                else
                    return vecSpeed;
            }
        }

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
            return false;
        }
        protected virtual bool CheckBoost()
        {
            return false;
        }
        protected virtual bool CheckQuick()
        {
            return false;
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

        protected virtual void UpdateInput()
        {
            UpdateXZ(out inputX, out inputZ);

            if (CheckJump())
            {
                if (isJump == false && isTouched)
                {
                    Vector3 holNor;
                    Vector3 local;
                    if (inputX == 0 && inputZ == 0)
                    {
                        holNor = Vector3.zero;
                        local = Vector3.up;
                    }
                    else
                    {
                        holNor = new Vector3(inputX, 0, inputZ);
                        holNor.Normalize();
                        local = holNor;
                        holNor = rigid.transform.TransformDirection(holNor);
                    }

                    jumpInfo.SetJump(KickAndJump(holNor), local);
                    rigid.velocity = Vector3.zero;
                }
                else
                {
                    Debug.LogFormat("IsJump:{0} IsTouched:{1}", isJump, isTouched);
                }
            }
            if (CheckBoost())
            {
                this.IsBoost = !this.IsBoost;
            }
            if (CheckQuick())
            {
                if (isQuick == false)
                {
                    Vector3 localVec;
                    Vector3 jumpVec;
                    if (inputX == 0 && inputZ == 0)
                    {
                        localVec = Vector3.forward;
                    }
                    else
                    {
                        localVec = new Vector3(inputX, 0, inputZ);
                        localVec.Normalize();
                        jumpVec = rigid.transform.TransformDirection(localVec);
                    }

                    var speed = quickSpeed;
                    if (isTouched)
                        speed *= touchQuickRate;

                    quickInfo.SetJump(jumpVector * quickSpeed, localVec);
                    rigid.velocity = Vector3.zero;
                }
                else
                {
                    Debug.LogFormat("IsQuick:{0} IsTouched:{1}", isQuick, isTouched);
                }
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

        private void UpdateMove()
        {
            bool isInput = inputX != 0 || inputZ != 0;
            Vector3 hVec = Vector3.zero;

            if (isInput)
            {
                hVec = new Vector3(inputX, 0, inputZ);
                hVec.Normalize();
                hVec = rigid.transform.TransformDirection(hVec);
            }

            bool canAccel = isInput;

            var vel = rigid.velocity;
            var dotVec = vel.x * hVec.x + vel.z * hVec.z;

            canAccel &= dotVec < LimitSpeed;
            canAccel &= !isJump || !isWallTouched;
            canAccel &= !isFloating || this.IsBoost;

            Vector3 vec = Vector3.zero;

            if (canAccel)
            {
                var accelTime = accelerateTime;
                if (this.isFloating)
                    accelTime *= 2;

                hVec *= vecSpeed * Time.deltaTime / accelTime;

                if (this.IsBoost)
                    hVec *= horizontalBoostRate;

                vec += hVec;

                if (this.isWallTouched)
                {
                    var dot = Vector3.Dot(touchChecker.CurrentNormal, vec.normalized);
                    if (dot < 0)
                    {
                        var normal = -vec.magnitude * touchChecker.CurrentNormal * dot;
                        vec += new Vector3(normal.x, 0, normal.z);
                    }
                }
            }

            var delta = Time.deltaTime;
            if (isJump)
            {
                bool forceReset = false;
                if (jumpSumTime < touchTime)
                    vec += jumpVector * delta / touchTime;
                else
                    forceReset = isTouched;

                jumpSumTime += delta;
                if (jumpSumTime >= jumpTime || forceReset)
                {
                    jumpInfo.ResetJump();
                }
            }

            if (isQuick)
            {
                bool forceReset = false;
                if (quickSumTime < quickTime)
                    vec += quickVector * delta / quickTime;
                else
                    forceReset = isTouched;

                quickSumTime += delta;
                if (quickSumTime >= quickTime || forceReset)
                {
                    quickInfo.ResetJump();
                }
            }

            if (this.IsBoost && !isInput && !isJump)
            {
                vec = new Vector3(-vel.x, 0, -vel.z);
                var brake = this.airBrakeRate;

                if (this.isTouched)
                {
                    brake += this.touchBrakeRateAdd;
                }

                vec *= brake * delta;
            }

            if (isFloating && this.IsBoost && vel.y < 0)
            {
                vec += -1.0f * Vector3.up * Physics.gravity.y * delta * floatRate;
            }

            rigid.AddForce(vec, ForceMode.VelocityChange);
        }

        private void UpdateEffect()
        {
            int bit = 0;
            if (IsBoost)
            {
                if (isFloating)
                {
                    bit = -1;
                }
                else
                {
                    bit = ActionUtils.ConvertBitXZ(inputX, inputZ);
                }
            }

            partsContainer.Boost(bit);

            if (isQuick && quickInfo.jumpSumTime == 0)
            {
                partsContainer.Quick(ActionUtils.ConvertBit(quickInfo.localNor));
            }

            if (isJump && jumpInfo.jumpSumTime == 0)
            {
                partsContainer.Quick(ActionUtils.ConvertBit(jumpInfo.localNor));
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
            var mX = cameraVec2.x;//InputUtils.GetCenterMouse().x;
            if (mX * mX < rotXMin * rotXMin)
                return;

            var rotDeg = mX * rotSpeed * Time.deltaTime;

            rigid.transform.Rotate(Vector3.up * rotDeg);
        }

        private Vector3 KickAndJump(Vector3 holNor)
        {
            if (touchChecker == null)
                return Vector3.zero;

            var touch = touchChecker.CurrentTouchState;
            var normal = touchChecker.CurrentNormal;
            switch (touch)
            {
                case TouchState.Wall:
                    if (holNor == Vector3.zero ||
                        Vector3.Dot(holNor, normal) < 0) {
                        return Vector3.up * jumpSpeed * wallJumpRate;
                    }
                    else {
                        return holNor * jumpSpeed;
                    }

                case TouchState.Ground:
                    return Vector3.up * jumpSpeed;

                default:
                    return Vector3.zero;
            }
        }
    }
}
