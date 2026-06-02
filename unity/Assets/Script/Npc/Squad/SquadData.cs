using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 一部隊のランタイムデータ。
    /// SquadManager が生成・保持し、ISquadMember が参照する。
    /// </summary>
    public class SquadData
    {
        public int SquadID { get; }
        public UnitSide Side { get; }
        public SquadOrder CurrentOrder { get; private set; } = SquadOrder.Idle;

        // リーダー（このSquadの移動目的地を決定するユニット）
        public ISquadMember Leader { get; private set; }

        // 偵察等が共有する目標座標
        public Vector3? SharedTarget { get; private set; }

        private readonly List<ISquadMember> members = new List<ISquadMember>();
        public IReadOnlyList<ISquadMember> Members => members;

        public SquadData(int squadId, UnitSide side)
        {
            SquadID = squadId;
            Side    = side;
        }

        public void AddMember(ISquadMember member)
        {
            if (!members.Contains(member))
                members.Add(member);
        }

        public void RemoveMember(ISquadMember member)
        {
            members.Remove(member);
            if (Leader == member)
                Leader = null;
        }

        public void SetLeader(ISquadMember leader)
        {
            Leader = leader;
        }

        public void SetOrder(SquadOrder order)
        {
            CurrentOrder = order;
        }

        public void SetSharedTarget(Vector3 target)
        {
            SharedTarget = target;
        }

        public void ClearSharedTarget()
        {
            SharedTarget = null;
        }

        /// <summary>部隊の平均座標（支援ユニットの追従先などに使用）</summary>
        public Vector3 GetCenterPosition()
        {
            if (members.Count == 0) return Vector3.zero;
            var sum = Vector3.zero;
            int count = 0;
            foreach (var m in members)
            {
                if (m is MonoBehaviour mb && mb != null)
                {
                    sum += mb.transform.position;
                    count++;
                }
            }
            return count > 0 ? sum / count : Vector3.zero;
        }
    }
}
