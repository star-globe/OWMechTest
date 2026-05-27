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
    public bool IsInCircle { get; private set; } = false;
    public float ScreenDistanceSqr { get; private set; } = float.MaxValue;
    public float WorldDistanceSqr { get; private set; } = float.MaxValue;
    public bool IsTargetActive
    {
        get
        {
            return target != null && target.gameObject.activeSelf;
        }
    }
    public BaseObject Target => target;

    public void SetTarget(BaseObject tgt, Camera cam, float circleSize)
    {
        this.target = tgt;
        this.worldCam = cam;
        this.circleSize = circleSize;
        IsInside = true;
        UpdateRectPosition();
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
            IsInCircle = false;
            ScreenDistanceSqr = float.MaxValue;
            WorldDistanceSqr = float.MaxValue;
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

        ScreenDistanceSqr = pos.x * pos.x + pos.y * pos.y;
        var halfCircle = 0.5f * circleSize;
        bool inCircle = ScreenDistanceSqr < halfCircle * halfCircle;
        IsInCircle = IsInside && inCircle;

        var toTarget = targetPos - worldCam.transform.position;
        WorldDistanceSqr = toTarget.sqrMagnitude;
        bool blocked = Physics.Raycast(worldCam.transform.position, toTarget.normalized, toTarget.magnitude, GameLayers.RaycastObstacleMask);

        squareImage.color = blocked ? blockedColor : (inCircle ? inCircleColor : outCircleColor);
    }
}
