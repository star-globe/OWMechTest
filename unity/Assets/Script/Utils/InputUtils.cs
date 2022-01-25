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

        var posX = Mathf.Clamp(pos.x / x, -1.0f, 1.0f);
        var posY = Mathf.Clamp(pos.y / y, -1.0f, 1.0f);
        pos = new Vector3(posX, posY, 0);

        return pos;
    }
}
