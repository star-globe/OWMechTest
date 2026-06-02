using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ミッション選択画面。
    /// FieldSelectMenu の代わりにミッション単位で選択し、ブリーフィング画面へ遷移する。
    /// </summary>
    public class MissionSelectMenu : MonoBehaviour
    {
        [SerializeField]
        ObjectPool<MissionSelectButton> pool;

        private void Awake()
        {
            pool.Initialize();
        }

        private void Start()
        {
            ShowMissions();
        }

        private void ShowMissions()
        {
            pool.ReturnAll();

            var missionDic = MissionMaster.Instance.GetDictionary();
            foreach (var kvp in missionDic)
            {
                var mission = kvp.Value;
                var field   = FieldMaster.Instance.GetSettings(mission.FieldId);
                var fieldName = field != null ? field.FieldName : $"Field {mission.FieldId}";

                var button = pool.Borrow();
                button.SetData(mission.MissionId, mission.MissionName, fieldName);
                button.SetOnClick(SelectMission);
            }
        }

        private void SelectMission(int missionId)
        {
            var mission = MissionMaster.Instance.GetSettings(missionId);
            if (mission == null) return;

            FieldManager.Instance.SetPendingMission(missionId);
            StateManager.Instance.NextState();    // Select → Briefing
        }
    }
}
