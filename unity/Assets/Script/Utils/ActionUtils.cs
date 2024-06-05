using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

public static class ActionUtils
{
    public const int TimeDenominator = 100;
    public static int ReloadInterLimit
    {
        get
        {
            //20FPS
            return TimeDenominator / 20;
        }
    }

    public static int ConvertBit(Vector3 v)
    {
        if (v.y > 0)
        {
            return -1;
        }

        return ConvertBitXZ(v.x, v.z);
    }
    public static int ConvertBitXZ(float x, float z)
    {
        int bit = 0;
        if (x > 0)
        {
            bit |= 1 >> (byte) BoostVector.Right;
        }
        else if (x < 0)
        {
            bit |= 1 >> (byte) BoostVector.Left;
        }

        if (z > 0)
        {
            bit |= 1 >> (byte) BoostVector.Forward;
        }
        else if (z < 0)
        {
            bit |= 1 >> (byte) BoostVector.Back;
        }

        return bit;
    }
}
