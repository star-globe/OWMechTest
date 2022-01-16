using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class InputUtils
{
    public static Vector3 GetCenterMouse()
    {
        if (Input.mousePresent == false)
            return Vector3.zero;

        var x = Screen.width / 2;
        var y = Screen.height / 2;

        var pos = Input.mousePosition;
        pos -= new Vector3(x, y, 0);
        pos = new Vector3(pos.x / x, pos.y / y, 0);

        return pos;
    }
}
