using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : SingletonMonobehaviour<PlayerManager>
{
    [SerializeField]
    GameObject playerPrefab;

    public PlayerCharacter MyPC { get; private set; }

    private readonly Dictionary<long, PlayerCharacter> playerDictionary = new Dictionary<long, PlayerCharacter>();

    public PlayerCharacter CreatePlayer(bool isSelf, long id, Vector3 position, Quaternion rot)
    {
        var go = Instantiate(playerPrefab, position, rot);
        var pc = go.GetComponent<PlayerCharacter>();
        if (pc == null)
            return null;

        pc.Initialize(id, isSelf);

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
