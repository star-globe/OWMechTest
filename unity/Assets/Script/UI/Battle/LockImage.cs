using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdvancedGears;

public class LockImage : MonoBehaviour
{
    [SerializeField]
    Image squareImage;

    [SerializeField]
    Color inCircleColor;

    [SerializeField]
    Color outCircleColor;

    [SerializeField]
    Color blockedColor;

    BaseObject target;
    public long ID
    {
        get
        {
            if (target == null)
                return -1;

            return target.ID;
        }
    }

    Camera worldCam;

    float circleSize = 0f;

    RectTransform rect = null;
    RectTransform RectTransform
    {
        get
        {
            rect = rect ?? this.transform as RectTransform;
            return rect;
        }
    }

    public bool IsInside { get; private set; } = false;

    public void SetTarget(BaseObject tgt, Camera cam, float circleSize)
    {
        this.target = tgt;
        this.worldCam = cam;
        this.circleSize = circleSize;
        IsInside = true;
    }

    private void LateUpdate()
    {
        UpdateRectPosition();
    }

    private void UpdateRectPosition()
    {
        if (target == null || worldCam == null)
        {
            IsInside = true;
            return;
        }

        var center = 0.5f * new Vector3(Screen.width, Screen.height);
        var widthSqrMax = center.x * center.x;
        var heightSqrMax = center.y * center.y;

        var targetPos = target.transform.position + target.SelfHeight * 0.5f * Vector3.up;

        var pos = worldCam.WorldToScreenPoint(targetPos) - center;

        this.RectTransform.anchoredPosition = pos;

        IsInside = pos.z >= 0 &&
                   pos.x * pos.x <= widthSqrMax &&
                   pos.y * pos.y <= heightSqrMax;

        var halfCircle = 0.5f * circleSize;
        bool inCircle = (pos.x * pos.x + pos.y * pos.y) < halfCircle * halfCircle;

        var toTarget = targetPos - worldCam.transform.position;
        bool blocked = Physics.Raycast(worldCam.transform.position, toTarget.normalized, toTarget.magnitude, GameLayers.RaycastObstacleMask);

        squareImage.color = blocked ? blockedColor : (inCircle ? inCircleColor : outCircleColor);
    }
}
