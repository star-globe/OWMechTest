using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BattleUIManager : SingletonMonoBehaviour<BattleUIManager>
    {
        public long CurrentPlayerId { get; private set; }

        public void SetPlayerId(long playerId)
        {
            CurrentPlayerId = playerId;
        }
    }
}
