using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField]
    CharacterController controller = null;

    [SerializeField]
    Transform target = null;

    [SerializeField]
    float targetLength = 5.0f;

    [SerializeField]
    float moveTime = 0.5f;

    [SerializeField]
    float cameraDiffMin = 0.01f;

    [SerializeField]
    float forwardDiffMin = 0.01f;

    [SerializeField]
    float rotSpeed = 90.0f;

    [SerializeField]
    float rotLim = 60.0f;

    float defLength;
    Camera cam;

    void Start()
    {
        defLength = (this.transform.position - controller.transform.position).magnitude;
    }

    void FixedUpdate()
    {
        cam = cam ?? Camera.main;
        if (cam == null)
            return;

        UpdateCameraPos(cam.transform);
        UpdateCameraTarget(cam.transform);
        RotateTarget();
    }

    private void UpdateCameraPos(Transform cam)
    {
        var current = cam.position;
        var target = this.transform.position;
        var controllerPos = controller.transform.position;

        var diff = target - current;
        if (diff.sqrMagnitude <= cameraDiffMin * cameraDiffMin)
            return;

        diff *= Time.deltaTime / moveTime;
        var p = current + diff;
        var vec = (p - controllerPos).normalized * defLength;
        cam.transform.position = vec + controllerPos;
    }

    private void RotateTarget()
    {
        var foward = (target.position - controller.transform.position).normalized;
        var mY = InputUtils.GetCenterMouse().y;

        var rotMin = Mathf.Sin(rotLim * Mathf.Deg2Rad);

        if (mY > 0 && foward.y > rotMin)
            return;

        if (mY < 0 && foward.y < -rotMin)
            return;

        var deg = mY * rotSpeed * Time.deltaTime;

        var quo = Quaternion.AngleAxis(-deg, controller.transform.right);
        foward = quo * foward;

        target.position = controller.transform.position + foward * targetLength;
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
