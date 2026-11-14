using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// NPC の部隊を 1 つ、自身の位置・回転を基準にスポーンさせるコンポーネント。ミッションのシーンに置く。
    /// FixedUnitSpawner に倣い、マスターデータのロード完了を待ってからスポーンする
    /// </summary>
    public class NpcSpawner : MonoBehaviour
    {
        [Serializable]
        public class Member
        {
            [SerializeField]
            SquadRole role = SquadRole.Vanguard;
            public SquadRole Role => role;

            [SerializeField]
            [Tooltip("スポーナーから見た位置（ローカル座標、m）")]
            Vector3 offset;
            public Vector3 Offset => offset;
        }

        // ── Inspector ────────────────────────────────────
        [SerializeField]
        UnitSide unitSide = UnitSide.Bravo;

        [SerializeField]
        [Tooltip("部隊のメンバー。役割が Leader の最初のメンバーを部隊のリーダーにする")]
        List<Member> members = new List<Member>();

        [SerializeField]
        SquadOrder order = SquadOrder.Advance;

        [SerializeField]
        [Tooltip("部隊の移動目的地（未設定なら目的地なし。目標を見つけたら向かう）")]
        Transform destination;

        [SerializeField]
        [Tooltip("目的地にこの距離まで近づいたら止まる（m）")]
        float goalDistance = 30f;

        [SerializeField]
        float inter = 0.1f;

        // ── Internal ─────────────────────────────────────
        private WaitForSeconds waiter;
        private WaitForSeconds Waiter => waiter ??= new WaitForSeconds(inter);

        // 接地点を探すときに、スポーン位置のどれだけ上から下へ調べるか（m）
        const float GroundCheckHeight = 1.0f;

        // ── Lifecycle ─────────────────────────────────────
        private void Start()
        {
            StartCoroutine(WaitAndSpawn());
        }

        // ── Coroutine ─────────────────────────────────────
        private IEnumerator WaitAndSpawn()
        {
            // PlayerMaster に NPC のプレハブが登録されるまで待機
            while (PlayerMaster.GetPlayerPrefab(PlayerType.NPC) == null)
            {
                yield return Waiter;
            }

            SpawnSquad(PlayerMaster.GetPlayerPrefab(PlayerType.NPC));
        }

        // ── Spawn ─────────────────────────────────────────
        private void SpawnSquad(GameObject prefab)
        {
            var squad = SquadManager.Instance.CreateSquad(unitSide);
            var rot = transform.rotation;

            foreach (var member in members)
            {
                var pos = GetGroundedPos(transform.TransformPoint(member.Offset));
                var npc = NpcManager.Instance.CreateNpc(unitSide, pos, rot, prefab, squad, member.Role);
                if (npc == null)
                    continue;

                if (squad.Leader == null && member.Role == SquadRole.Leader)
                    squad.SetLeader(npc.GetComponent<NpcController>());
            }

            squad.SetOrder(order);

            if (destination != null)
                SquadManager.Instance.AssignDestinationToSquad(squad.SquadID, destination.position, goalDistance);
        }

        private static Vector3 GetGroundedPos(Vector3 pos)
        {
            if (Physics.Raycast(new Ray(pos + Vector3.up * GroundCheckHeight, Vector3.down), out var hit))
                return hit.point;

            return pos;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            foreach (var member in members)
                Gizmos.DrawWireSphere(transform.TransformPoint(member.Offset), 2f);

            if (destination != null)
                Gizmos.DrawLine(transform.position, destination.position);
        }
    }
}
