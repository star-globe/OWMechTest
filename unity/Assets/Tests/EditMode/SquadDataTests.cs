using NUnit.Framework;
using UnityEngine;

namespace AdvancedGears.Tests
{
    public class SquadDataTests
    {
        /// <summary>MonoBehaviour でない ISquadMember（座標を持たない）</summary>
        class FakeMember : ISquadMember
        {
            public SquadRole Role => default;
            public float DefaultRadius => 10f;
            public void SetDestination(Vector3 destination, float goalDistance) { }
            public void ClearDestination() { }
            public void SetSquad(SquadData squad, SquadRole role) { }
        }

        [Test]
        public void AddMember_IgnoresDuplicates()
        {
            var squad = new SquadData(1, default);
            var m = new FakeMember();

            squad.AddMember(m);
            squad.AddMember(m);

            Assert.AreEqual(1, squad.Members.Count);
        }

        [Test]
        public void RemoveMember_ClearsLeaderWhenLeaderRemoved()
        {
            var squad = new SquadData(1, default);
            var leader = new FakeMember();
            var other = new FakeMember();
            squad.AddMember(leader);
            squad.AddMember(other);
            squad.SetLeader(leader);

            squad.RemoveMember(other);
            Assert.AreSame(leader, squad.Leader);

            squad.RemoveMember(leader);
            Assert.IsNull(squad.Leader);
            Assert.AreEqual(0, squad.Members.Count);
        }

        [Test]
        public void SharedTarget_SetAndClear()
        {
            var squad = new SquadData(1, default);
            Assert.IsNull(squad.SharedTarget);

            squad.SetSharedTarget(new Vector3(1, 2, 3));
            Assert.AreEqual(new Vector3(1, 2, 3), squad.SharedTarget);

            squad.ClearSharedTarget();
            Assert.IsNull(squad.SharedTarget);
        }

        [Test]
        public void SetOrder_ChangesCurrentOrder()
        {
            var squad = new SquadData(1, default);
            Assert.AreEqual(SquadOrder.Idle, squad.CurrentOrder);

            squad.SetOrder(SquadOrder.Advance);
            Assert.AreEqual(SquadOrder.Advance, squad.CurrentOrder);
        }

        [Test]
        public void GetCenterPosition_IgnoresMembersWithoutTransform()
        {
            var squad = new SquadData(1, default);
            Assert.AreEqual(Vector3.zero, squad.GetCenterPosition());

            squad.AddMember(new FakeMember());
            Assert.AreEqual(Vector3.zero, squad.GetCenterPosition());
        }
    }
}
