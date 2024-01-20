using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class InputXZReciever : InputXZComponent
    {
        float input_x;
        float input_z;

        public void SetInputXZ(float inputX, float inputZ)
        {
            this.input_x = inputX;
            this.input_z = inputZ;
        }

        public override void InputXZ(out float inputX, out float inputZ)
        {
            inputX = input_x;
            inputZ = input_z;
        }
    }
}
