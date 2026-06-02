using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 全部隊を横断管理するシングルトン。
    /// 部隊の生成・命令発令・リーダーによる目的地配布を担う。
    /// </summary>
    public class SquadManager : SingletonMonoBehaviour<SquadManager>
    {
        private readonly Dictionary<int, SquadData> squadDic = new Dictionary<int, SquadData>();
        private int nextSquadId = 1;

        // ─── 部隊操作 ─────────────────────────────────────────

        public SquadData CreateSquad(UnitSide side)
        {
            var squad = new SquadData(nextSquadId++, side);
            squadDic[squad.SquadID] = squad;
            return squad;
        }

        public void DisbandSquad(int squadId)
        {
            squadDic.Remove(squadId);
        }

        public SquadData GetSquad(int squadId)
        {
            squadDic.TryGetValue(squadId, out var squad);
            return squad;
        }

        /// <summary>指定 side の全部隊に命令を発令する。</summary>
        public void IssueOrderToSide(UnitSide side, SquadOrder order)
        {
            foreach (var squad in squadDic.Values)
            {
                if (squad.Side == side)
                    squad.SetOrder(order);
            }
        }

        public void IssueOrder(int squadId, SquadOrder order)
        {
            if (squadDic.TryGetValue(squadId, out var squad))
                squad.SetOrder(order);
        }

        /// <summary>
        /// 上位リーダーとして、部隊のリーダーユニットに移動目的地を指定する。
        /// リーダーが受け取った目的地は ISquadMember.SetDestination 経由で下位に伝播する。
        /// </summary>
        public void AssignDestinationToSquad(int squadId, Vector3 destination, float goalDistance)
        {
            if (!squadDic.TryGetValue(squadId, out var squad)) return;
            squad.Leader?.SetDestination(destination, goalDistance);
        }

        // ─── 毎フレーム更新 ──────────────────────────────────

        private void Update()
        {
            foreach (var squad in squadDic.Values)
                UpdateSquad(squad);
        }

        private void UpdateSquad(SquadData squad)
        {
            // 偵察ユニットが発見した目標を部隊全体で共有する
            Vector3? scoutTarget = null;
            foreach (var member in squad.Members)
            {
                if (member.Role == SquadRole.Scout &&
                    member is NpcController nc &&
                    nc.LastKnownTarget.HasValue)
                {
                    scoutTarget = nc.LastKnownTarget;
                    break;
                }
            }

            if (scoutTarget.HasValue)
                squad.SetSharedTarget(scoutTarget.Value);
            else
                squad.ClearSharedTarget();
        }
    }
}
