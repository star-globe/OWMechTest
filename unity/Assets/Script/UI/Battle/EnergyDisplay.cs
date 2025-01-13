using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public class EnergyDisplay : MonoBehaviour
    {
        [SerializeField]
        Slider slider = null;

        [SerializeField]
        TextMeshProUGUI valueText = null;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        int energy = int.MinValue;
        int maxEnergy = int.MinValue;

        void Update()
        {
            float value = 0;
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                value = 1.0f * param.Energy / param.MaxEnergy;

                if (energy != param.Energy || maxEnergy != param.MaxEnergy)
                {
                    energy = param.Energy;
                    maxEnergy = param.MaxEnergy;

                    valueText.SetText($"{energy}/{maxEnergy}");
                }
            }

            slider.value = value;
        }
    }
}

