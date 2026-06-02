using System;
using UnityEngine;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面のパーツ選択ボタン。
    /// パーツIDとパーツ名を表示し、選択時にコールバックを呼ぶ。
    /// </summary>
    public class PartsSelectButton : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI partsNameText;

        int partId;
        Action<int> callback;

        public void SetData(int partId, string partsName, Action<int> callback)
        {
            this.partId = partId;
            this.callback = callback;

            if (partsNameText != null)
                partsNameText.SetText(partsName);
        }

        public void OnClick()
        {
            callback?.Invoke(partId);
        }
    }
}
