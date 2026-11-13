using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public class ArmorDisplay : MonoBehaviour
    {
        [SerializeField]
        Slider slider = null;

        [SerializeField]
        TextMeshProUGUI apText = null;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        int currentAp = -1;

        void Update()
        {
            int ap = 0;
            float value = 0;
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                ap = param.Ap;
                // int どうしで割ると 0 か 1 になるため、float で割る
                value = param.MaxAp > 0 ? (float) param.Ap / param.MaxAp : 0f;
            }

            slider.value = value;

            if (currentAp != ap)
            {
                apText.SetText(ap.ToString());
                currentAp = ap;
            }
        }
    }
}

