using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public class BoostDisplay : MonoBehaviour
    {
        enum State : byte
        {
            None,
            On,
            Off,
        }

        [SerializeField]
        TextMeshProUGUI text = null;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        State boostState = State.None;

        void Update()
        {
            State st = State.Off;
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                st = param.IsBoost ? State.On: State.Off;
            }

            if (boostState != st)
            {
                text.enabled = st == State.On;
                boostState = st;
            }
        }
    }
}

