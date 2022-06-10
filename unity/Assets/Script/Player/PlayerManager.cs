using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PlayerManager : SingletonMonoBehaviour<PlayerManager>
    {
        public PlayerCharacter MyPC { get; private set; }

        private readonly Dictionary<long, PlayerCharacter> playerDictionary = new Dictionary<long, PlayerCharacter>();

        public PlayerCharacter CreatePlayer(bool isSelf, long id, Vector3 position, Quaternion rot)
        {
            var prefab = PlayerMaster.Instance.GetPlayerPrefab(isSelf ? PlayerType.Self: PlayerType.Other);
            if (prefab == null)
                return null;

            var go = Instantiate(prefab, position, rot);
            var pc = go.GetComponent<PlayerCharacter>();
            if (pc == null)
                return null;

            pc.Initialize(id, isSelf);
            go.transform.position += pc.PlayerHeight * Vector3.up;

            if (isSelf)
            {
                MyPC = pc;
            }
            else
            {
                playerDictionary[id] = pc;
            }

            return pc;
        }
    }
}
