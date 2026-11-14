using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class NpcManager : SingletonMonoBehaviour<NpcManager>
    {
        // NPC の機体（NPCPlayer）はプレイヤーの機体と同じ PlayerCharacter を持つ（IsSelf = false）
        private readonly Dictionary<long, PlayerCharacter> npcDictionary = new Dictionary<long, PlayerCharacter>();

        /// <summary>
        /// NPCを生成する。squad と role を渡すと諸兵科連合に参加させる。
        /// 固定ユニット等、squad に参加させたくない場合は null を渡す。
        /// position は接地点。機体の高さの分だけ持ち上げて置く（PlayerManager.CreatePlayer と同じ）。
        /// ID は固定ユニット（FixedUnitSpawner）と同じく、生成した GameObject の EntityId から作る
        /// </summary>
        public PlayerCharacter CreateNpc(UnitSide side,
                                         Vector3 position, Quaternion rot,
                                         GameObject prefab,
                                         SquadData squad = null, SquadRole role = SquadRole.None)
        {
            if (prefab == null)
            {
                Debug.LogError("[NpcManager] prefab is null.");
                return null;
            }

            var go  = Instantiate(prefab, position, rot);
            var npc = go.GetComponent<PlayerCharacter>();
            if (npc == null)
            {
                Debug.LogErrorFormat("[NpcManager] PlayerCharacter not found on prefab: {0}", prefab.name);
                Destroy(go);
                return null;
            }

            var id = (long)EntityId.ToULong(go.GetEntityId());
            npc.Initialize(id, side, isSelf: false);
            go.transform.position += npc.PlayerHeight * Vector3.up;
            npcDictionary[id] = npc;

            if (squad != null)
            {
                var controller = go.GetComponent<NpcController>();
                if (controller != null)
                    controller.SetSquad(squad, role);
                else
                    Debug.LogErrorFormat("[NpcManager] NpcController not found on prefab: {0}", prefab.name);
            }

            return npc;
        }

        public PlayerCharacter GetNpc(long id)
        {
            npcDictionary.TryGetValue(id, out var npc);
            return npc;
        }

        public void RemoveNpc(long id)
        {
            if (npcDictionary.TryGetValue(id, out var npc))
            {
                if (npc != null) Destroy(npc.gameObject);
                npcDictionary.Remove(id);
            }
        }
    }
}
