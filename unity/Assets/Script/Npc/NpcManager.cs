using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class NpcManager : SingletonMonoBehaviour<NpcManager>
    {
        private readonly Dictionary<long, NpcCharacter> npcDictionary = new Dictionary<long, NpcCharacter>();

        // ─── NPC 生成 ─────────────────────────────────────────

        public NpcCharacter CreateNpc(long id, UnitSide side, int logicId,
                                      Vector3 position, Quaternion rot,
                                      GameObject prefab,
                                      SquadData squad = null, SquadRole role = SquadRole.None)
        {
            if (prefab == null)
            {
                Debug.LogError("[NpcManager] prefab is null.");
                return null;
            }

            var go = Instantiate(prefab, position, rot);
            var npc = go.GetComponent<NpcCharacter>();
            if (npc == null)
            {
                Debug.LogErrorFormat("[NpcManager] NpcCharacter not found on prefab: {0}", prefab.name);
                Destroy(go);
                return null;
            }

            npc.Initialize(id, side, logicId);
            npcDictionary[id] = npc;

            // 部隊に参加
            if (squad != null)
            {
                var controller = go.GetComponent<NpcController>();
                if (controller != null)
                    controller.JoinSquad(squad, role);
            }

            return npc;
        }

        public NpcCharacter GetNpc(long id)
        {
            npcDictionary.TryGetValue(id, out var npc);
            return npc;
        }

        public void RemoveNpc(long id)
        {
            if (npcDictionary.TryGetValue(id, out var npc))
            {
                if (npc != null)
                    Destroy(npc.gameObject);
                npcDictionary.Remove(id);
            }
        }
    }
}
