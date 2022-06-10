using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "TestProject/Input/InputSettings", order = 0)]
public class InputSettings : ScriptableObject
{
    [SerializeField]
    float inputXMin = 0.2f;

    [SerializeField]
    float inputYMin = 0.2f;

    [SerializeField]
    float rotLim = 60.0f;

    public bool CanRotateY(float mY, float rotDeg, out float val)
    {
        bool canRotate = mY * mY >= inputYMin * inputYMin;

        if (mY > 0)
        {
            canRotate &= rotDeg < rotLim;
            val = mY - inputYMin;
        }
        else
        {
            canRotate &= rotDeg > -rotLim;
            val = mY + inputYMin;
        }

        return canRotate;
    }

    public bool CanRotateX(float mX, out float val)
    {
        bool canRotate = mX * mX >= inputXMin * inputXMin;
        if (mX > 0)
        {
            val = mX - inputXMin;
        }
        else
        {
            val = mX + inputXMin;
        }

        return canRotate;
    }
}
