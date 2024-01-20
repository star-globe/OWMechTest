using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class NpcManager : SingletonMonoBehaviour<NpcManager>
    {
        public NpcCharacter CompanyNpc { get; private set; }

        private readonly Dictionary<long, NpcCharacter> npcDictionary = new Dictionary<long, NpcCharacter>();

        public NpcCharacter CreateNpc(bool isCompany, long id, UnitSide side, Vector3 position, Quaternion rot)
        {
            //var prefab = PlayerMaster.GetPlayerPrefab(isSelf ? PlayerType.Self: PlayerType.Other);
            //if (prefab == null)
            //    return null;
//
            //var go = Instantiate(prefab, position, rot);
            //var pc = go.GetComponent<PlayerCharacter>();
            //if (pc == null)
            //    return null;
//
            //pc.Initialize(id, side, isSelf);
            //go.transform.position += pc.PlayerHeight * Vector3.up;
//
            //if (isSelf)
            //{
            //    MyPC = pc;
            //}
            //else
            //{
            //    playerDictionary[id] = pc;
            //}

            return null;
        }
    }
}
