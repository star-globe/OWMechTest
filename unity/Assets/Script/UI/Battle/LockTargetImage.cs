using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AdvancedGears;

namespace AdvancedGears
{
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
        [Tooltip("二次ロック（偏差射撃）になった武器の Image の色。一次ロックでは color を使う")]
        Color secondaryLockColor = new Color(1f, 0.35f, 0.2f, 1f);

        [SerializeField]
        Color textBackgroundColor = Color.clear;

        string textBackgroundTag = string.Empty;

        BaseObject target;
        Camera worldCam;
        PlayerController controller;

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


        public void SetTarget(BaseObject tgt, Camera cam, PlayerController lockController)
        {
            this.target = tgt;
            this.worldCam = cam;
            this.controller = lockController;
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
            if (controller == null)
                return;

            ApplyLockLevel(leftLockImage, controller.GetLockLevel(WeaponSlot.Left));
            ApplyLockLevel(rightLockImage, controller.GetLockLevel(WeaponSlot.Right));
        }

        /// <summary>一次ロックは color、二次ロックは secondaryLockColor で表示する。ロックしていなければ消す</summary>
        private void ApplyLockLevel(Image image, LockLevel level)
        {
            if (image == null)
                return;

            image.enabled = level != LockLevel.None;
            if (level != LockLevel.None)
                image.color = level == LockLevel.Secondary ? secondaryLockColor : color;
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
                var ap = target.Ap;
                var maxAp = target.MaxAp;
                if (ap != int.MinValue && maxAp != int.MinValue)
                {
                    int hp = ap;
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
}
