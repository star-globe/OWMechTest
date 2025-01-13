using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class MyPlayerController : PlayerController
    {
        [SerializeField]
        float defaultTargetLength = 100;

        [SerializeField]
        float defaultTargetAngle = 45.0f;

        protected override void UpdateXZ(out float inputX, out float inputZ)
        {
            InputUtils.UpdateXZ(out inputX, out inputZ);
        }

        protected override Vector2 GetCameraPos()
        {
            return InputUtils.GetCenterMouse();
        }

        protected override bool CheckBoost()
        {
            return base.CheckBoost() && InputUtils.CheckBoost();
        }

        protected override bool CheckJump()
        {
            return base.CheckJump() && InputUtils.CheckJump();
        }

        protected override bool CheckQuick()
        {
            return base.CheckQuick() && InputUtils.CheckQuick();
        }

        protected override bool CheckHyperBoost()
        {
            return base.CheckHyperBoost() && InputUtils.CheckHyperBoost();
        }

        private void SetTarget(out Vector3 target)
        {
            var trans = this.gameObject.transform;
            if (PhysicsUtils.CheckOverlapScorn(trans.position, trans.forward * defaultTargetLength, defaultTargetAngle * Mathf.Deg2Rad, this.UnitSide, -1, "Player", out target) == false)
            {
                target = trans.position + trans.forward * defaultTargetLength + Vector3.up * this.PlayerHeight;
            }
        }

        protected override bool CheckLeftFire(out Vector3 target)
        {
            SetTarget(out target);

            return InputUtils.CheckLeftFire();
        }

        protected override bool CheckRightFire(out Vector3 target)
        {
            SetTarget(out target);

            return InputUtils.CheckRightFire();
        }
    }
}
