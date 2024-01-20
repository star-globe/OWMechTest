using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : SingletonMonoBehaviour<PlayerInputManager>
{
    private MainControls _mainControls = null;

    public MainControls MainControls => _mainControls;

    void Awake()
    {
        _mainControls = new MainControls();
        _mainControls.Enable();
    }
}
