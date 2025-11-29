using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class PlayerInput : MonoBehaviour
    {
        struct JumpInfo
        {
            public bool isJump { get; private set; }
            public float jumpSumTime { get; private set; }
            public Vector3 jumpVector { get; private set; }

            public void SetJump(Vector3 vector)
            {
                isJump = true;
                jumpSumTime = 0.0f;
                jumpVector = vector;
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

        [SerializeField]
        Rigidbody rigid = null;

        [SerializeField]
        TouchChecker touchChecker = null;

        [SerializeField]
        float vecSpeed = 5.0f;

        [SerializeField]
        float accelerateTime = 1.0f;

        [SerializeField]
        float jumpSpeed = 10.0f;

        [SerializeField]
        float rotXMin = 0.2f;

        [SerializeField]
        float wallJumpRate = 0.5f;

        [SerializeField]
        float jumpTime = 0.5f;

        [SerializeField]
        float touchTimeRate = 0.2f;
        float touchTime => jumpTime * touchTimeRate;

        [SerializeField]
        float horizontalBoostRate = 1.5f;

        [SerializeField]
        float floatRate = 0.9f;

        [SerializeField]
        PlayerControllerSettings controllerSettings;

        float rotSpeed => controllerSettings.RotSpeed;

        bool isBoost = false;
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

        float inputX = 0;
        float inputZ = 0;

        private void Update()
        {
            UpdateInput();
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
                if (isBoost)
                    return vecSpeed * horizontalBoostRate;
                else
                    return vecSpeed;
            }
        }

        private void UpdateInput()
        {
            InputUtils.UpdateXZ(out inputX, out inputZ);

            if (isJump == false && isTouched)
            {
                if (InputUtils.CheckJump())
                {
                    Vector3 holNor;
                    if (inputX == 0 && inputZ == 0)
                        holNor = Vector3.zero;
                    else {
                        holNor = new Vector3(inputX, 0, inputZ);
                        holNor.Normalize();
                        holNor = rigid.transform.TransformDirection(holNor);
                    }

                    jumpInfo.SetJump(KickAndJump(holNor));
                    rigid.linearVelocity = Vector3.zero;
                }
            }

            if (InputUtils.CheckBoost())
            {
                isBoost = !isBoost;
            }
        }

        private void UpdateMove()
        {
            bool canAccel = inputX != 0 || inputZ != 0;

            var vel = rigid.linearVelocity;
            var sqr = vel.x * vel.x + vel.z * vel.z;

            canAccel &= sqr < LimitSpeed * LimitSpeed;

            canAccel &= !isJump || !isWallTouched;

            Vector3 vec = Vector3.zero;

            if (canAccel)
            {
                var hVec = new Vector3(inputX, 0, inputZ);
                hVec.Normalize();
                hVec = rigid.transform.TransformDirection(hVec);
                hVec *= vecSpeed * Time.deltaTime / accelerateTime;

                if (isBoost)
                    hVec *= horizontalBoostRate;

                vec += hVec;

                if (this.isWallTouched) {
                    var dot = Vector3.Dot(touchChecker.CurrentNormal, vec.normalized);
                    if (dot < 0) {
                        var normal = -vec.magnitude * touchChecker.CurrentNormal * dot;
                        vec += new Vector3(normal.x, 0, normal.z);
                    }
                }
            }

            var delta = Time.deltaTime;
            if (isJump)
            {
                if (jumpSumTime < touchTime)
                    vec += jumpVector * delta / touchTime;

                jumpSumTime += delta;
                if (jumpSumTime >= jumpTime)
                {
                    jumpInfo.ResetJump();
                }
            }

            if (isFloating && isBoost && vel.y < 0)
            {
                vec += -1.0f * Vector3.up * Physics.gravity.y * delta * floatRate;
            }

            rigid.AddForce(vec, ForceMode.VelocityChange);
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
            var mX = InputUtils.GetCenterMouse().x;
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
