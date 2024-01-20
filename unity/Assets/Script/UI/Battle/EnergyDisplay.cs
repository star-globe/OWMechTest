using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    public class EnergyDisplay : MonoBehaviour
    {
        [SerializeField]
        Slider slider = null;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        void Update()
        {
            float value = 0;
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                value = param.Energy / param.MaxEnergy;
            }

            slider.value = value;
        }
    }
}

