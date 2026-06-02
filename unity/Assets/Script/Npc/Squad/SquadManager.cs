using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 全部隊を横断的に管理するシングルトン。
    /// 部隊の生成・命令発令・解散を担う。
    /// </summary>
    public class SquadManager : SingletonMonoBehaviour<SquadManager>
    {
        private readonly Dictionary<int, SquadData> squadDic = new Dictionary<int, SquadData>();
        private int nextSquadId = 1;

        // ─── 部隊操作 ─────────────────────────────────────────

        /// <summary>新しい部隊を作成して返す。</summary>
        public SquadData CreateSquad(UnitSide side)
        {
            var squad = new SquadData(nextSquadId++, side);
            squadDic[squad.SquadID] = squad;
            return squad;
        }

        /// <summary>部隊を解散する。</summary>
        public void DisbandSquad(int squadId)
        {
            squadDic.Remove(squadId);
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

        /// <summary>指定部隊 ID に命令を発令する。</summary>
        public void IssueOrder(int squadId, SquadOrder order)
        {
            if (squadDic.TryGetValue(squadId, out var squad))
                squad.SetOrder(order);
        }

        public SquadData GetSquad(int squadId)
        {
            squadDic.TryGetValue(squadId, out var squad);
            return squad;
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
                if (member.Role == SquadRole.Scout && member.LastKnownTarget.HasValue)
                {
                    scoutTarget = member.LastKnownTarget;
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
