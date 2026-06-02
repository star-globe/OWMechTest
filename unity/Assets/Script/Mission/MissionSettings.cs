using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ミッションマスターデータ。
    /// フィールドとは独立したデータとして管理し、同一フィールドに複数ミッションが存在できる。
    /// </summary>
    [CreateAssetMenu(menuName = "TestProject/Mission/MissionSettings", order = 0)]
    public class MissionSettings : ScriptableObject, IDBasedMasterSettings
    {
        [Header("識別")]
        [SerializeField]
        int missionId;
        public int MissionId => missionId;

        [SerializeField]
        string missionName;
        public string MissionName => missionName;

        [Header("使用フィールド")]
        [SerializeField]
        int fieldId;
        /// <summary>使用する FieldSettings の FieldID</summary>
        public int FieldId => fieldId;

        [Header("目標情報")]
        [SerializeField]
        [TextArea(2, 6)]
        string missionObjective;
        public string MissionObjective => missionObjective;

        [Header("制限時間（秒、0 = 無制限）")]
        [SerializeField]
        float timeLimitSeconds = 0f;
        public float TimeLimitSeconds => timeLimitSeconds;
        public bool HasTimeLimit => timeLimitSeconds > 0f;

        [Header("ミッション依存シーン（配置物）")]
        [SerializeField]
        List<string> missionSceneNames = new List<string>();
        /// <summary>
        /// このミッション固有の配置物を持つシーン名一覧。
        /// フィールドシーンに Additive でロードされる。
        /// </summary>
        public List<string> MissionSceneNames => missionSceneNames;

        // IDBasedMasterSettings
        public int ID => missionId;

#if UNITY_EDITOR
        public void SearchMissionScenes()
        {
            // エディタ補助：指定フォルダからシーンを自動収集する場合に使用
        }
#endif
    }

    public class MissionMasterContainer : IDBasedMasterContainer<MissionSettings>
    {
        protected override string resourcesFolder => "MissionSettings";
    }

    public class MissionMaster : Singleton<MissionMasterContainer>
    {
        public static MissionSettings GetSettings(int missionId)
        {
            return Instance.GetSettings(missionId);
        }
    }
}
