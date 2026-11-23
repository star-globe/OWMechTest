using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace AdvancedGears
{
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

            // ゲーム画面が背面にある間（Windows キーなど）は Input System がデバイスをリセットし、
            // ポインタ位置が (0, 0)＝左下として読まれてカメラが動くため、入力なしとして扱う
            if (!Application.isFocused)
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

            if (IsWaitingMouseSync())
            {
                return Vector3.zero;
            }

            var mouse = _mainControls.Player.CameraMouse;
            var pos = mouse.ReadValue<Vector2>();

            var x = Screen.width / 2;
            var y = Screen.height / 2;

            pos -= new Vector2(x, y);

            var posX = Mathf.Clamp(pos.x / x, -1.0f, 1.0f);
            var posY = Mathf.Clamp(pos.y / y, -1.0f, 1.0f);
            pos = new Vector3(posX, posY, 0);

            return pos;
        }

        /// <summary>
        /// フォーカスが戻ったときにポインタを画面中心へ移動するか（戦闘中だけ true にする。メニューではボタン操作の邪魔になる）
        /// </summary>
        public static bool RecenterOnFocus { get; set; } = false;

        static bool waitingMouseSync = false;
        static double focusRegainedTime = 0.0;

        /// <summary>
        /// フォーカスの切り替わりを受け取る（<see cref="PlayerInputManager"/> の OnApplicationFocus から呼ぶ）。
        /// 背面にある間に Input System がマウスを初期状態（位置 (0, 0)）に戻し、フォーカスが戻っても
        /// マウスから位置の通知が来るまで (0, 0) のままになるため、通知が届くまでカメラ入力を 0 にする
        /// </summary>
        public static void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                return;
            }

            waitingMouseSync = true;
            focusRegainedTime = InputState.currentTime;
        }

        /// <summary>フォーカスが戻ってから、マウスの位置の通知をまだ受け取っていないか</summary>
        static bool IsWaitingMouseSync()
        {
            if (!waitingMouseSync)
            {
                return false;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.lastUpdateTime > focusRegainedTime)
            {
                waitingMouseSync = false;
            }

            return waitingMouseSync;
        }

        /// <summary>
        /// マウスポインタを画面中心へ移動する。
        /// カメラはポインタの中心からのずれで回るため、シーン移行時に呼んで意図しない回転を防ぐ。
        /// ゲームパッド使用時（<see cref="GetCenterMouse"/> がスティックを読むとき）は何もしない
        /// </summary>
        public static void WarpMouseToCenter()
        {
            if (Gamepad.current != null)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            mouse.WarpCursorPosition(new Vector2(Screen.width / 2f, Screen.height / 2f));
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
        }

        public static bool CheckJump()
        {
            var action = _mainControls.Player.Jump;
            return action.WasPressedThisFrame();
        }

        /// <summary>ジャンプボタンを押し続けているか（グライド中の上昇に使う）</summary>
        public static bool CheckJumpHold()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.Jump;
            return action.IsPressed();
        }

        public static bool CheckQuick()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.Quick;
            return action.WasPressedThisFrame();
        }

        public static bool CheckBoost()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.Boost;
            return action.WasPressedThisFrame();
        }

        public static bool CheckRightFire()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.RightFire;
            return action.IsPressed();
        }

        public static bool CheckLeftFire()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.LeftFire;
            return action.IsPressed();
        }

        public static bool CheckHyperBoost()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.HyperBoost;
            return action.WasPressedThisFrame();
        }

        public static bool CheckModeSwitch()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.ModeSwitch;
            return action.WasPressedThisFrame();
        }

        public static bool CheckSubFire()
        {
            if (_mainControls == null)
            {
                return false;
            }

            var action = _mainControls.Player.SubFire;
            return action.WasPressedThisFrame();
        }
    }
}
