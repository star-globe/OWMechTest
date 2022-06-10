using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class GoalObject : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            var character = collision.gameObject.GetComponent<PlayerCharacter>();
            if (character == null)
            {
                return;
            }

            StateManager.Instance.NextState();
        }
    }
}
