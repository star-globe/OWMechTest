using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdvancedGears;

public class LockTargetImage : MonoBehaviour
{
    [SerializeField]
    Image lockCircleImage;

    [SerializeField]
    Image leftLockImage;

    [SerializeField]
    Image rightLockImage;

    [SerializeField]
    TextMeshProUGUI distanceText;

    [SerializeField]
    TextMeshProUGUI hpText;

    [SerializeField]
    Color color = Color.white;

    [SerializeField]
    Color textBackgroundColor = Color.clear;

    string textBackgroundTag = string.Empty;

    BaseObject target;
    Camera worldCam;
    CurrentLockStateInfo lockStateInfo;

    int prevDistanceTenth = int.MinValue;
    int prevHp = int.MinValue;

    RectTransform rect = null;
    RectTransform RectTransform
    {
        get
        {
            rect = rect ?? this.transform as RectTransform;
            return rect;
        }
    }

    private void Awake()
    {
        ApplyColor();
    }

    public void ApplyColor()
    {
        if (lockCircleImage != null)
            lockCircleImage.color = color;

        if (leftLockImage != null)
            leftLockImage.color = color;

        if (rightLockImage != null)
            rightLockImage.color = color;

        if (distanceText != null)
            distanceText.color = color;

        if (hpText != null)
            hpText.color = color;

        textBackgroundTag = $"<mark=#{ColorUtility.ToHtmlStringRGBA(textBackgroundColor)}>";
    }

#if UNITY_EDITOR
    public void ApplyTextBackGroundColor()
    {
        if (distanceText != null)
        {
            distanceText.text = $"{textBackgroundTag}9999</mark>";
        }

        if (hpText != null)
        {
            hpText.text = $"{textBackgroundTag}999999</mark>";
        }
    }
#endif

    public void SetTarget(BaseObject tgt, Camera cam, CurrentLockStateInfo stateInfo)
    {
        this.target = tgt;
        this.worldCam = cam;
        this.lockStateInfo = stateInfo;
        prevDistanceTenth = int.MinValue;
        prevHp = int.MinValue;
    }

    private void LateUpdate()
    {
        UpdateRectPosition();
        UpdateLockImages();
        UpdateInfo();
    }

    private void UpdateLockImages()
    {
        if (lockStateInfo == null)
            return;

        if (leftLockImage != null)
            leftLockImage.enabled = lockStateInfo.IsLeftLocked;

        if (rightLockImage != null)
            rightLockImage.enabled = lockStateInfo.IsRightLocked;
    }

    private void UpdateRectPosition()
    {
        if (target == null || worldCam == null)
            return;

        var center = 0.5f * new Vector3(Screen.width, Screen.height);
        var targetPos = target.transform.position + target.SelfHeight * 0.5f * Vector3.up;
        var pos = worldCam.WorldToScreenPoint(targetPos) - center;

        this.RectTransform.anchoredPosition = pos;
    }

    private void UpdateInfo()
    {
        if (target == null || worldCam == null)
            return;

        var targetPos = target.transform.position + target.SelfHeight * 0.5f * Vector3.up;

        if (distanceText != null)
        {
            float dist = Vector3.Distance(worldCam.transform.position, targetPos);
            int distTenth = Mathf.RoundToInt(dist * 10);
            if (distTenth != prevDistanceTenth)
            {
                prevDistanceTenth = distTenth;
                dist *= GlobalParamMaster.Instance.WorldSizeRate;
                distanceText.text = $"{textBackgroundTag}{dist.ToString("F1")}</mark>";
            }
        }

        if (hpText != null)
        {
            var chara = target as BaseCharacter;
            if (chara != null && chara.CharacterParam != null)
            {
                int hp = chara.CharacterParam.Ap;
                if (hp != prevHp)
                {
                    prevHp = hp;
                    hpText.text = $"{textBackgroundTag}{hp}</mark>";
                }
            }
            else
            {
                prevHp = int.MinValue;
                hpText.text = string.Empty;
            }
        }
    }
}
