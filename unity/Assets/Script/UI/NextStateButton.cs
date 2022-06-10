using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AdvancedGears
{
    public class NextStateButton : MonoBehaviour
    {
        public void GoToNextState()
        {
            StateManager.Instance.NextState();
        }
    }
}
