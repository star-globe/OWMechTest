using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class DeadGoalComponent : DeadComponent
    {
        public override void DeadAction()
        {
            base.DeadAction();
            StateManager.Instance.NextState();
        }
    }
}
