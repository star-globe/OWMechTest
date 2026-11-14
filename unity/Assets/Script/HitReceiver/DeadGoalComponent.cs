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

            // 自機が撃破された後（演出中）に目標を壊しても、任務は失敗のまま
            var state = StateManager.Instance;
            if (state.TryDecideMissionResult(MissionResult.Success))
                state.FinishBattle();
        }
    }
}
