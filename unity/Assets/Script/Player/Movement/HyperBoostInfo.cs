using UnityEngine;

namespace AdvancedGears
{
    public struct HyperBoostInfo
    {
        public bool isOn { get; private set; }
        public Vector2 inputVector { get; private set; }
        public Vector3 localBoostVector { get; private set; }

        public void SetBoost(Vector2 input, float boostVal)
        {
            inputVector = input;
            localBoostVector = new Vector3(input.x, 0, input.y) * boostVal;
            isOn = true;
        }

        public void ResetBoost()
        {
            localBoostVector = Vector3.zero;
            isOn = false;
        }

        public bool CheckInputVector(Vector2 input)
        {
            return Vector2.Dot(input, inputVector) >= 0;
        }
    }
}
