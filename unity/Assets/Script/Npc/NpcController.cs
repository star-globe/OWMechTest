using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class NpcController : PlayerController
    {
        [SerializeField]
        float radius = 100;

        [SerializeField]
        NavMeshAgent agent;

        int? _enemyLayerMask = null;
        int EnemyLayerMask => _enemyLayerMask ??= LayerMask.GetMask(GameLayers.MyPlayer, GameLayers.OtherPlayer, GameLayers.NonPlayer, GameLayers.Unit);

        private bool CheckTarget(out Vector3 target)
        {
            var pos = this.transform.position;
            // 敵味方情報が必要
            return PhysicsUtils.CheckOverlapShpereOthers(pos, radius, this.UnitSide, EnemyLayerMask, string.Empty, out target);
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

        protected override void UpdateInput()
        {
            UpdateAgent();

            base.UpdateInput();
        }

        Vector3[] corners = new Vector3[16];

        private void UpdateAgent()
        {
            if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
            {
                agent.path.GetCornersNonAlloc(corners);
            }
        }
    }
}
