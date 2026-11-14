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

        [SerializeField]
        [Tooltip("オーバーヒート中に赤く明滅させるゲージの Graphic（背景・Fill など）")]
        Graphic[] overheatGraphics = null;

        [SerializeField]
        Color overheatColor = Color.red;

        [SerializeField, Min(0.01f)]
        [Tooltip("オーバーヒート中の明滅の周期（秒）")]
        float overheatBlinkPeriod = 0.5f;

        long playerId
        {
            get
            {
                return BattleUIManager.Instance.CurrentPlayerId;
            }
        }

        int energy = int.MinValue;
        int maxEnergy = int.MinValue;

        Color[] defaultColors = null;
        bool isOverheatShown = false;

        void Awake()
        {
            if (overheatGraphics == null)
                return;

            defaultColors = new Color[overheatGraphics.Length];
            for (int i = 0; i < overheatGraphics.Length; i++)
                defaultColors[i] = overheatGraphics[i] != null ? overheatGraphics[i].color : Color.white;
        }

        void Update()
        {
            float value = 0;
            bool isOverheat = false;
            float overheatElapsed = 0f;
            var param = PlayerManager.Instance.GetPlayer(playerId)?.CharacterParam;
            if (param != null)
            {
                value = 1.0f * param.Energy / param.MaxEnergy;
                isOverheat = param.IsOverheat;
                overheatElapsed = param.OverheatElapsed;

                if (energy != param.Energy || maxEnergy != param.MaxEnergy)
                {
                    energy = param.Energy;
                    maxEnergy = param.MaxEnergy;

                    valueText.SetText($"{energy}/{maxEnergy}");
                }
            }

            // オーバーヒート中は、残量が 0 から復帰エネルギーまでゆっくり増える（CharacterParam.Recovery）ので、
            // ゲージの伸びで解除までの時間が分かる
            slider.value = value;

            UpdateOverheatColor(isOverheat, overheatElapsed);
        }

        void UpdateOverheatColor(bool isOverheat, float elapsed)
        {
            if (overheatGraphics == null || defaultColors == null)
                return;

            if (!isOverheat && !isOverheatShown)
                return;

            var rate = isOverheat ? CalcOverheatBlinkRate(elapsed, overheatBlinkPeriod) : 0f;
            for (int i = 0; i < overheatGraphics.Length; i++)
            {
                var graphic = overheatGraphics[i];
                if (graphic != null)
                    graphic.color = Color.Lerp(defaultColors[i], overheatColor, rate);
            }

            isOverheatShown = isOverheat;
        }

        /// <summary>
        /// オーバーヒート中の明滅で、赤に寄せる割合（0〜1）。オーバーヒートした瞬間は 1（赤）で、
        /// period 秒ごとに 1 → 0 → 1 と滑らかに変わる
        /// </summary>
        public static float CalcOverheatBlinkRate(float elapsed, float period)
        {
            if (period <= 0f)
                return 1f;

            return 0.5f + 0.5f * Mathf.Cos(2f * Mathf.PI * elapsed / period);
        }
    }
}
