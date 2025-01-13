using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public class SpeedDisplay : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI text = null;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        int displaySpeed = int.MinValue;

        void Update()
        {
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                var val = (int) (param.Speed * 100);
                if (displaySpeed != val)
                {
                    displaySpeed = val;

                    text.SetText((val * 0.01f).ToString("F2"));
                }
            }
        }
    }
}

