using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField]
    Transform center = null;

    [SerializeField]
    Transform target = null;

    [SerializeField]
    float targetLength = 5.0f;

    [SerializeField]
    float moveTime = 0.5f;

    [SerializeField]
    float cameraDiffMin = 0.01f;

    [SerializeField]
    float camDeltaRadius = 1.0f;

    [SerializeField]
    float powerRate = 2.0f;

    [SerializeField]
    float forwardDiffMin = 0.01f;

    [SerializeField]
    float rotSpeed = 90.0f;

    [SerializeField]
    float rotLim = 60.0f;

    [SerializeField]
    float inputYMin = 0.2f;

    float defLength;
    float rotDeg = 0.0f;
    Vector3 defLocal;
    Camera cam;

    void Start()
    {
        defLength = (this.transform.position - center.position).magnitude;
        defLocal = this.transform.localPosition;
    }

    void FixedUpdate()
    {
        cam = cam ?? Camera.main;
        if (cam == null)
            return;

        UpdateCameraPos(cam.transform);
        UpdateCameraTarget(cam.transform);
        UpdateTarget();
    }

    private void UpdateCameraPos(Transform cam)
    {
        var current = cam.position;
        var target = this.transform.position;
        var controllerPos = center.position;

        var diff = target - current;
        if (diff.sqrMagnitude <= cameraDiffMin * cameraDiffMin)
            return;

        diff -= diff.normalized * cameraDiffMin;

        var rate = (Time.deltaTime / moveTime) * Mathf.Pow((diff.magnitude / camDeltaRadius), powerRate);
        rate = Mathf.Min(rate, 1.0f);

        diff *= rate;
        //var p = current + diff;
        //var vec = (p - controllerPos).normalized * defLength;
        //cam.transform.position = vec + controllerPos;
        cam.transform.position = current + diff;
    }

    private void UpdateTarget()
    {
        var mY = InputUtils.GetCenterMouse().y;

        bool canRotate = mY * mY >= inputYMin * inputYMin;

        if (mY > 0)
            canRotate &= rotDeg < rotLim;
        else
            canRotate &= rotDeg > -rotLim;

        if (canRotate)
        {
            rotDeg += mY * rotSpeed * Time.deltaTime;
        }

        var quo = Quaternion.AngleAxis(-rotDeg, Vector3.right);
        var foward = quo * Vector3.forward;
        foward = center.localToWorldMatrix * foward;

        this.transform.localPosition = quo * defLocal;

        target.position = center.position + foward * targetLength;
    }

    private void UpdateCameraTarget(Transform cam)
    {
        var current = cam.position;
        var camFoward = cam.forward;

        var foward = target.position - current;
        var forwardDiff = foward - camFoward;
        if (forwardDiff.sqrMagnitude <= forwardDiffMin * forwardDiffMin)
            return;

        var tgt = camFoward * targetLength + current;
        var diff = target.position - tgt;
        diff *= Time.deltaTime / moveTime;
        tgt += diff;

        cam.forward = (tgt - current).normalized;
    }
}
