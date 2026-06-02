using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 検証シーン用のシンプルなスタイル切り替えUI。
/// Canvas > Panel にボタンを2つ置いてアサインしてください。
/// </summary>
public class GraphicsTestUI : MonoBehaviour
{
    [SerializeField] GraphicsStyleSwitcher[] switchers;
    [SerializeField] Button greyboxButton;
    [SerializeField] Button stylizedButton;
    [SerializeField] TMP_Text currentStyleLabel;

    void Start()
    {
        greyboxButton?.onClick.AddListener(OnGreybox);
        stylizedButton?.onClick.AddListener(OnStylized);
        RefreshLabel();
    }

    void OnGreybox()
    {
        foreach (var s in switchers) s.SwitchToGreybox();
        RefreshLabel();
    }

    void OnStylized()
    {
        foreach (var s in switchers) s.SwitchToStylizedReal();
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (currentStyleLabel == null || switchers.Length == 0) return;
        currentStyleLabel.text = $"Style: {switchers[0].CurrentStyle}";
    }
}
