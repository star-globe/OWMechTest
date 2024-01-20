using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class NpcController : PlayerController
    {
        [SerializeField]
        float radius = 100;

        private bool CheckTarget(out Vector3 target)
        {
            var pos = this.transform.position;
            // 敵味方情報が必要
            return PhysicsUtils.CheckOverlapShpere(pos, radius, this.UnitSide, -1, "Player", out target);
        }

        protected override bool CheckLeftFire(out Vector3 target)
        {
            if (CheckTarget(out target))
            {
                return true;    // NOTE:判定処理の追加
            }
            else
            {
                return false;
            }
        }

        protected override bool CheckRightFire(out Vector3 target)
        {
            if (CheckTarget(out target))
            {
                return true;    // NOTE:判定処理の追加
            }
            else
            {
                return false;
            }
        }
    }
}
