using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    public enum PlayerType
    {
        None = 0,
        Self,
        Other,
        NPC,
    }

    [CreateAssetMenu(menuName = "TestProject/Player/PlayerSettings", order = 0)]
    public class PlayerSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField]
        PlayerType playerType;
        public PlayerType PlayerType => playerType;

        [SerializeField]
        GameObject playerPrefab;
        public GameObject PlayerPrefab => playerPrefab;

        public int ID => (int) playerType;
    }

    public class PlayerMasterContainer : IDBasedMasterContainer<PlayerSettings>
    {
        protected override string resourcesFolder => "PlayerSettings";
    }

    public class PlayerMaster : Singleton<PlayerMasterContainer>
    {
        public static GameObject GetPlayerPrefab(PlayerType type)
        {
            return Instance.GetSettings((int) type)?.PlayerPrefab;
        }
    }
}
