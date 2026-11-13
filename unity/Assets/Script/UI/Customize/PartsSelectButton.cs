using System;
using UnityEngine;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面のパーツ選択ボタン。
    /// パーツ名とエネルギーの値（常時消費など）を表示し、選択時にコールバックを呼ぶ。
    /// </summary>
    public class PartsSelectButton : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI partsNameText;
        [SerializeField] TextMeshProUGUI energyText;

        int partId;
        Action<int> callback;

        public void SetData(PartSettings settings, Action<int> callback)
        {
            this.partId = settings.PartID;
            this.callback = callback;

            if (partsNameText != null)
                partsNameText.SetText(settings.name);

            if (energyText != null)
                energyText.SetText(FormatEnergy(settings));
        }

        public void OnClick()
        {
            callback?.Invoke(partId);
        }

        private static string FormatEnergy(PartSettings settings)
        {
            switch (settings)
            {
                case GeneratorSettings generator:
                    return CustomizeEnergyText.FormatPartEnergy(generator.ConstantEnergyConsumption,
                                                                generator: (generator.EnergyCapacity, generator.EnergyOutput));
                case BoosterSettings booster:
                    return CustomizeEnergyText.FormatPartEnergy(booster.ConstantEnergyConsumption,
                                                                boostOnConsumption: booster.BoostOnEnergyConsumption);
                default:
                    return CustomizeEnergyText.FormatPartEnergy(settings.ConstantEnergyConsumption);
            }
        }
    }
}
