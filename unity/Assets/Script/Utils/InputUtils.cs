using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

public static class InputUtils
{
    private static MainControls _mainControls
    {
        get { return PlayerInputManager.Instance.MainControls; }
    }

    public static Vector3 GetCenterMouse()
    {
        if (_mainControls == null)
        {
            return Vector3.zero;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            var stick = _mainControls.Player.CameraStick;
            var vec = stick.ReadValue<Vector2>();
            return new Vector3(vec.x, vec.y, 0);
        }

        var mouse = _mainControls.Player.CameraMouse;
        var pos = mouse.ReadValue<Vector2>();

        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    var stick = pad.rightStick;
        //    return new Vector3(stick.x.value, stick.y.value, 0);
        //}
        //
        //var pos = Mouse.current.position.ReadValue();

        var x = Screen.width / 2;
        var y = Screen.height / 2;
        
        pos -= new Vector2(x, y);

        var posX = Mathf.Clamp(pos.x / x, -1.0f, 1.0f);
        var posY = Mathf.Clamp(pos.y / y, -1.0f, 1.0f);
        pos = new Vector3(posX, posY, 0);

        return pos;
    }

    const float inputLim = 0.1f;

    public static void UpdateXZ(out float x, out float z)
    {
        if (_mainControls == null)
        {
            x = 0;
            z = 0;
            return;
        }

        var action = _mainControls.Player.Move;
        var vec = action.ReadValue<Vector2>();

        x = vec.x;
        z = vec.y;

        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    var stick = pad.leftStick;
        //    x = stick.x.value;
        //    z = stick.y.value;
        //    return;
        //}
        //
        //var w = Keyboard.current.wKey.ReadValue();
        //var s = Keyboard.current.sKey.ReadValue();
        //var a = Keyboard.current.aKey.ReadValue();
        //var d = Keyboard.current.dKey.ReadValue();

        //z = w - s;
        //x = d - a;

        //if (z * z < inputLim * inputLim)
        //    z = 0;
        //
        //if (x * x < inputLim * inputLim)
        //    x = 0;
    }

    public static bool CheckJump()
    {
        var action = _mainControls.Player.Jump;
        return action.WasPressedThisFrame();
        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    return pad.rightShoulder.wasPressedThisFrame;
        //}
        //
        //return Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    public static bool CheckQuickMove()
    {
        if (_mainControls == null)
        {
            return false;
        }

        var action = _mainControls.Player.Quick;
        return action.WasPressedThisFrame();
        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    return pad.leftShoulder.wasPressedThisFrame;
        //}
        //
        //return Keyboard.current.leftShiftKey.wasPressedThisFrame;
    }

    public static bool CheckBoost()
    {
        if (_mainControls == null)
        {
            return false;
        }

        var action = _mainControls.Player.Boost;
        return action.WasPressedThisFrame();
        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    return pad.bButton.wasPressedThisFrame;
        //}
        //
        //return Keyboard.current.tabKey.wasPressedThisFrame;
    }

    public static bool CheckRightFire()
    {
        if (_mainControls == null)
        {
            return false;
        }

        var action = _mainControls.Player.RightFire;
        return action.IsPressed();

        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    return pad.rightTrigger.isPressed;
        //}
        //
        //return Mouse.current.rightButton.isPressed;
    }

    public static bool CheckLeftFire()
    {
        if (_mainControls == null)
        {
            return false;
        }

        var action = _mainControls.Player.LeftFire;
        return action.IsPressed();

        //var pad = Gamepad.current;
        //if (pad != null)
        //{
        //    return pad.leftTrigger.isPressed;
        //}
        //
        //return Mouse.current.leftButton.isPressed;
    }
}
