using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdvancedGears;

public class LockImage : MonoBehaviour
{
    [SerializeField]
    Image squareImage;

    BaseCharacter target;
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

    public void SetTarget(BaseCharacter tgt, Camera cam)
    {
        this.target = tgt;
        this.worldCam = cam;
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

        var pos = worldCam.WorldToScreenPoint(target.transform.position) - center;

        this.RectTransform.anchoredPosition = pos;

        IsInside = pos.z >= 0 &&
                   pos.x * pos.x <= widthSqrMax &&
                   pos.y * pos.y <= heightSqrMax;
    }
}
