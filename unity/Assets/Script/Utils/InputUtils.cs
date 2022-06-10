using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputUtils
{
    public static Vector3 GetCenterMouse()
    {
        var pos = Mouse.current.position.ReadValue();

        var x = Screen.width / 2;
        var y = Screen.height / 2;

        pos -= new Vector2(x, y);

        var posX = Mathf.Clamp(pos.x / x, -1.0f, 1.0f);
        var posY = Mathf.Clamp(pos.y / y, -1.0f, 1.0f);
        pos = new Vector3(posX, posY, 0);

        return pos;
    }
}
