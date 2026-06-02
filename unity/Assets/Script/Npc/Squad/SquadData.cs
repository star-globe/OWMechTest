using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 一部隊のランタイムデータ。
    /// SquadManager が生成・保持し、NpcController が参照する。
    /// </summary>
    public class SquadData
    {
        public int SquadID { get; }
        public UnitSide Side { get; }
        public SquadOrder CurrentOrder { get; private set; } = SquadOrder.Idle;

        // 共有目標座標（後衛・偵察がセットし、前衛が参照する）
        public Vector3? SharedTarget { get; private set; }

        private readonly List<NpcController> members = new List<NpcController>();
        public IReadOnlyList<NpcController> Members => members;

        public SquadData(int squadId, UnitSide side)
        {
            SquadID = squadId;
            Side    = side;
        }

        public void AddMember(NpcController controller)
        {
            if (!members.Contains(controller))
                members.Add(controller);
        }

        public void RemoveMember(NpcController controller)
        {
            members.Remove(controller);
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

        /// <summary>部隊の中心座標（支援ユニットの待機位置計算などに使用）</summary>
        public Vector3 GetCenterPosition()
        {
            if (members.Count == 0)
                return Vector3.zero;

            var sum = Vector3.zero;
            foreach (var m in members)
                sum += m.transform.position;
            return sum / members.Count;
        }
    }
}
