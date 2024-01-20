using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
}
