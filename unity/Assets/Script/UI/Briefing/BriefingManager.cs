using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    /// <summary>
    /// ブリーフィング画面のロジック。
    /// MissionSettings からミッション目標・制限時間・フィールド名を表示し、
    /// 出撃・カスタマイズ・戻るを制御する。
    /// </summary>
    public class BriefingManager : MonoBehaviour
    {
        [Header("表示UI")]
        [SerializeField] Text missionNameText;
        [SerializeField] Text fieldNameText;
        [SerializeField] Text missionObjectiveText;
        [SerializeField] Text timeLimitText;

        [Header("ボタン")]
        [SerializeField] Button sortieButton;
        [SerializeField] Button customizeButton;   // #8 実装後に接続
        [SerializeField] Button backButton;

        private void Start()
        {
            sortieButton?.onClick.AddListener(OnClickSortie);
            customizeButton?.onClick.AddListener(OnClickCustomize);
            backButton?.onClick.AddListener(OnClickBack);

            ShowMissionInfo();
        }

        private void ShowMissionInfo()
        {
            var missionId = FieldManager.Instance != null
                ? FieldManager.Instance.PendingMissionID
                : -1;

            if (missionId < 0)
            {
                Debug.LogWarning("[BriefingManager] PendingMissionID が未設定です。");
                return;
            }

            var mission = MissionMaster.Instance.GetSettings(missionId);
            if (mission == null)
            {
                Debug.LogErrorFormat("[BriefingManager] MissionSettings が見つかりません。ID:{0}", missionId);
                return;
            }

            if (missionNameText      != null) missionNameText.text      = mission.MissionName;
            if (missionObjectiveText != null) missionObjectiveText.text = mission.MissionObjective;

            if (timeLimitText != null)
            {
                timeLimitText.text = mission.HasTimeLimit
                    ? $"制限時間: {mission.TimeLimitSeconds:F0}秒"
                    : "制限時間: なし";
            }

            var field = FieldMaster.Instance.GetSettings(mission.FieldId);
            if (fieldNameText != null)
                fieldNameText.text = field != null ? field.FieldName : $"Field {mission.FieldId}";
        }

        /// <summary>出撃：フィールドとミッション依存シーンをロードして戦闘へ。</summary>
        private void OnClickSortie()
        {
            FieldManager.Instance?.LoadPendingMission();
            StateManager.Instance?.NextState();    // Briefing → Battle
        }

        /// <summary>カスタマイズ：#8 実装後に接続するプレースホルダー。</summary>
        private void OnClickCustomize()
        {
            // TODO: Issue #8 カスタマイズ画面の実装後にシーン遷移を実装する
            Debug.Log("[BriefingManager] カスタマイズ画面は未実装です。(Issue #8)");
        }

        /// <summary>戻る：ミッション選択画面に戻る。</summary>
        private void OnClickBack()
        {
            FieldManager.Instance?.SetPendingMission(-1);
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectMenu");
        }
    }
}
