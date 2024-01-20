using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class SimpleInputXZ : InputXZComponent
    {
        public override void InputXZ(out float inputX, out float inputZ)
        {
            InputUtils.UpdateXZ(out inputX, out inputZ);
        }
    }

    public abstract class InputXZComponent : MonoBehaviour
    {
        public abstract void InputXZ(out float inputX, out float inputZ);
    }
}
