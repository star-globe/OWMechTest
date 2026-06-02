using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    /// <summary>
    /// ブリーフィング画面のロジック。
    /// ステージ情報を表示し、カスタマイズ画面または出撃へ誘導する。
    /// </summary>
    public class BriefingManager : MonoBehaviour
    {
        [Header("表示UI")]
        [SerializeField] Text fieldNameText;
        [SerializeField] Text missionObjectiveText;
        [SerializeField] Text fieldIdText;

        [Header("ボタン")]
        [SerializeField] Button sortieButton;
        [SerializeField] Button customizeButton;   // #8 実装後に接続
        [SerializeField] Button backButton;

        private void Start()
        {
            SetupButtons();
            ShowFieldInfo();
        }

        private void SetupButtons()
        {
            sortieButton?.onClick.AddListener(OnClickSortie);
            customizeButton?.onClick.AddListener(OnClickCustomize);
            backButton?.onClick.AddListener(OnClickBack);
        }

        private void ShowFieldInfo()
        {
            var fieldId = FieldManager.Instance != null ? FieldManager.Instance.PendingFieldID : -1;
            if (fieldId < 0)
            {
                Debug.LogWarning("[BriefingManager] PendingFieldID が未設定です。");
                return;
            }

            var settings = FieldMaster.Instance.GetSettings(fieldId);
            if (settings == null)
            {
                Debug.LogErrorFormat("[BriefingManager] FieldSettings が見つかりません。ID:{0}", fieldId);
                return;
            }

            if (fieldNameText != null)
                fieldNameText.text = settings.FieldName;

            if (fieldIdText != null)
                fieldIdText.text = $"Field ID: {fieldId}";

            if (missionObjectiveText != null)
                missionObjectiveText.text = settings.MissionObjective;
        }

        /// <summary>出撃ボタン：フィールドをロードして戦闘へ進む。</summary>
        private void OnClickSortie()
        {
            FieldManager.Instance?.LoadPendingField();
            StateManager.Instance?.NextState();    // Briefing → Battle
        }

        /// <summary>カスタマイズボタン：#8 実装後に接続するプレースホルダー。</summary>
        private void OnClickCustomize()
        {
            // TODO: #8 カスタマイズ画面の実装後にシーン遷移を実装する
            Debug.Log("[BriefingManager] カスタマイズ画面は未実装です。(Issue #8)");
        }

        /// <summary>戻るボタン：ステージ選択に戻る。</summary>
        private void OnClickBack()
        {
            FieldManager.Instance?.SetPendingField(-1);
            // Select ステートへ直接遷移（NextState は Briefing→Battle なので SceneManager 経由で直接ロード）
            UnityEngine.SceneManagement.SceneManager.LoadScene("SelectMenu");
        }
    }
}
