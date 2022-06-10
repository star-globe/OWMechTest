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
    public class PlayerSettings : ScriptableObject
    {
        [SerializeField]
        PlayerType playerType;
        public PlayerType PlayerType => playerType;

        [SerializeField]
        GameObject playerPrefab;
        public GameObject PlayerPrefab => playerPrefab;
    }

    public class PlayerMaster : Singleton<PlayerMaster>, IMasterContainer
    {
        const string resourcesFolder = "PlayerSettings";

        public readonly Dictionary<PlayerType, PlayerSettings> SettingsDic = new Dictionary<PlayerType, PlayerSettings>();

        public void Load()
        {
            var settings = Resources.LoadAll<PlayerSettings>(resourcesFolder);

            SettingsDic.Clear();
            foreach (var set in settings)
            {
                SettingsDic[set.PlayerType] = set;
            }
        }

        public GameObject GetPlayerPrefab(PlayerType type)
        {
            if (this.SettingsDic.TryGetValue(type, out var set))
                return set.PlayerPrefab;

            return null;
        }
    }
}
