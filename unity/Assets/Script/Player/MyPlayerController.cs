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
            // 残量に関係なく切り替えられる
            return InputUtils.CheckBoost();
        }

        protected override bool CheckJump()
        {
            return base.CheckJump() && InputUtils.CheckJump();
        }

        protected override bool CheckAscend()
        {
            return InputUtils.CheckJumpHold();
        }

        protected override bool CheckQuick()
        {
            return base.CheckQuick() && InputUtils.CheckQuick();
        }

        protected override bool CheckHyperBoost()
        {
            return base.CheckHyperBoost() && InputUtils.CheckHyperBoost();
        }


        // ロックの目標は LockSiteDisplay（ロックサークル内でいちばん近い目標）が SetLockTarget で設定する。
        // ロックしていないときは正面へ撃つ
        protected override Vector3 GetFallbackAimPoint()
        {
            var trans = this.transform;
            return trans.position + trans.forward * defaultTargetLength + Vector3.up * this.PlayerHeight;
        }

        protected override bool CheckLeftFire()
        {
            return InputUtils.CheckLeftFire();
        }

        protected override bool CheckRightFire()
        {
            return InputUtils.CheckRightFire();
        }

        protected override bool CheckSubFire()
        {
            return InputUtils.CheckSubFire();
        }
    }
}
