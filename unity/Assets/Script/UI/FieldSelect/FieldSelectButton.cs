using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FieldSelectButton : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI fieldNameText = null;

    int fieldId = 0;
    Action<int> callBack = null;

    public void SetData(int fieldId, string fieldName)
    {
        if (fieldNameText != null)
            fieldNameText.SetText(fieldName);

        this.fieldId = fieldId;
    }

    public void SetOnClick(Action<int> callback)
    {
        this.callBack = callback;
    }

    public void OnSelect()
    {
        this.callBack?.Invoke(fieldId);
    }
}
