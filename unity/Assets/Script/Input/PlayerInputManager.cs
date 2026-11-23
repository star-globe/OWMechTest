using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PlayerInputManager : SingletonMonoBehaviour<PlayerInputManager>
    {
        private MainControls _mainControls = null;

        public MainControls MainControls => _mainControls;

        bool recenterPending = false;

        void Awake()
        {
            _mainControls = new MainControls();
            _mainControls.Enable();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            InputUtils.OnFocusChanged(hasFocus);

            // フォーカスが戻った直後は Input System がデバイスを有効に戻す前のことがあるため、
            // ポインタの移動は次の Update で行う
            if (hasFocus && InputUtils.RecenterOnFocus)
                recenterPending = true;
        }

        void Update()
        {
            if (!recenterPending)
                return;

            recenterPending = false;
            InputUtils.WarpMouseToCenter();
        }
    }
}
