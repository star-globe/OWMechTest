using TMPro;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// Result 画面の任務の結果の表示。今は任務失敗のときに「Failed」と出すだけ（細かい表示の仕様は別途決める。#108）
    /// </summary>
    public class ResultPanel : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI resultText;

        const string FailedText = "Failed";

        private void Start()
        {
            if (resultText == null)
                return;

            bool isFailed = StateManager.Instance.MissionResult == MissionResult.Failed;
            resultText.text = isFailed ? FailedText : string.Empty;
            resultText.gameObject.SetActive(isFailed);
        }
    }
}
