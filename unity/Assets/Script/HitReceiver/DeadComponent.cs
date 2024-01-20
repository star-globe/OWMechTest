using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class DeadComponent : MonoBehaviour
    {
        public virtual void DeadAction()
        {
            this.gameObject.SetActive(false);
        }
    }
}
