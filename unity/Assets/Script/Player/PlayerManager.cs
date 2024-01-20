using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PlayerManager : SingletonMonoBehaviour<PlayerManager>
    {
        public long MyPlayerId { get; private set; } = -1;

        public PlayerCharacter MyPC
        {
            get
            {
                if (MyPlayerId < 0)
                    return null;

                return playerDictionary[MyPlayerId];
            }
        }

        private readonly Dictionary<long, PlayerCharacter> playerDictionary = new Dictionary<long, PlayerCharacter>();

        public PlayerCharacter CreatePlayer(bool isSelf, long id, UnitSide side, Vector3 position, Quaternion rot)
        {
            var prefab = PlayerMaster.GetPlayerPrefab(isSelf ? PlayerType.Self: PlayerType.Other);
            if (prefab == null)
                return null;

            var go = Instantiate(prefab, position, rot);
            var pc = go.GetComponent<PlayerCharacter>();
            if (pc == null)
                return null;

            pc.Initialize(id, side, isSelf);
            go.transform.position += pc.PlayerHeight * Vector3.up;

            playerDictionary[id] = pc;

            if (isSelf)
            {
                MyPlayerId = id;
            }

            return pc;
        }

        public PlayerCharacter GetPlayer(long playerId)
        {
            if (playerDictionary.TryGetValue(playerId, out var playerChara))
            {
                return playerChara;
            }

            return null;
        }
    }
}
