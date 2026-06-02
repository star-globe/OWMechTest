using System;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    public class MissionSelectButton : MonoBehaviour
    {
        [SerializeField] Text missionNameText;
        [SerializeField] Text fieldNameText;
        [SerializeField] Button button;

        private int missionId;
        private Action<int> onClick;

        public void SetData(int id, string missionName, string fieldName)
        {
            missionId = id;
            if (missionNameText != null) missionNameText.text = missionName;
            if (fieldNameText   != null) fieldNameText.text   = fieldName;
        }

        public void SetOnClick(Action<int> callback)
        {
            onClick = callback;
            button?.onClick.RemoveAllListeners();
            button?.onClick.AddListener(() => onClick?.Invoke(missionId));
        }
    }
}
