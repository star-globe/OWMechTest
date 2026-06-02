using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面のスロット選択ボタン。
    /// スロット種別と現在装備中のパーツ名を表示する。
    /// </summary>
    public class PartsSlotButton : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI slotNameText;
        [SerializeField] TextMeshProUGUI currentPartsText;

        PartsAttachType slot;
        Action<PartsAttachType> callback;

        public void SetData(PartsAttachType slot, int currentPartId, Action<PartsAttachType> callback)
        {
            this.slot = slot;
            this.callback = callback;

            if (slotNameText != null)
                slotNameText.SetText(SlotDisplayName(slot));

            if (currentPartsText != null)
            {
                var partName = "（未装備）";
                if (currentPartId >= 0)
                {
                    var dic = PartMaster.Instance.GetDictionary();
                    if (dic != null && dic.TryGetValue(currentPartId, out var settings))
                        partName = settings.name;
                }
                currentPartsText.SetText(partName);
            }
        }

        public void OnClick()
        {
            callback?.Invoke(slot);
        }

        private static string SlotDisplayName(PartsAttachType slot)
        {
            switch (slot)
            {
                case PartsAttachType.Head:         return "頭部";
                case PartsAttachType.Core:         return "胴体";
                case PartsAttachType.Arm:          return "腕部";
                case PartsAttachType.Leg:          return "脚部";
                case PartsAttachType.Booster:      return "ブースター";
                case PartsAttachType.Weapon_Right: return "右腕武装";
                case PartsAttachType.Weapon_Left:  return "左腕武装";
                case PartsAttachType.Weapon_Sub:   return "サブ武装";
                default:                           return slot.ToString();
            }
        }
    }
}
