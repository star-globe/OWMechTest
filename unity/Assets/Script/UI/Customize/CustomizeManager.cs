using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面の全体制御。
    /// スロット選択パネルとパーツ選択パネルを管理し、変更を PlayerPrefs へ保存する。
    /// </summary>
    public class CustomizeManager : MonoBehaviour
    {
        // カスタマイズ完了後に戻るGameState（呼び出し元が設定）
        static GameState returnState = GameState.Select;

        public static void SetReturnState(GameState state) => returnState = state;

        [Header("スロット選択")]
        [SerializeField] ObjectPool<PartsSlotButton> slotPool;

        [Header("パーツ選択")]
        [SerializeField] ObjectPool<PartsSelectButton> partsPool;

        [Header("ボタン")]
        [SerializeField] Button saveButton;
        [SerializeField] Button backButton;

        [Header("データ")]
        [SerializeField] PlayerPartsAssembleDataAsset assembleDataAsset;

        // 編集中の組み合わせ（スロット→パーツID）
        readonly Dictionary<PartsAttachType, int> editingDic = new Dictionary<PartsAttachType, int>();

        PartsAttachType selectedSlot = PartsAttachType.None;

        static readonly PartsAttachType[] AllSlots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Leg,
            PartsAttachType.Booster,
            PartsAttachType.Weapon_Right,
            PartsAttachType.Weapon_Left,
            PartsAttachType.Weapon_Sub,
        };

        private void Start()
        {
            slotPool.Initialize();
            partsPool.Initialize();

            saveButton?.onClick.AddListener(OnClickSave);
            backButton?.onClick.AddListener(OnClickBack);

            LoadCurrentData();
            ShowSlots();
        }

        private void LoadCurrentData()
        {
            assembleDataAsset.Load();
            editingDic.Clear();

            var dic = assembleDataAsset.GetPartId_All();
            foreach (var slot in AllSlots)
            {
                editingDic[slot] = dic != null && dic.TryGetValue(slot, out var id) ? id : -1;
            }
        }

        private void ShowSlots()
        {
            slotPool.ReturnAll();
            foreach (var slot in AllSlots)
            {
                var btn = slotPool.Borrow();
                btn.SetData(slot, editingDic[slot], OnSelectSlot);
            }
        }

        private void OnSelectSlot(PartsAttachType slot)
        {
            selectedSlot = slot;
            ShowPartsForSlot(slot);
        }

        private void ShowPartsForSlot(PartsAttachType slot)
        {
            partsPool.ReturnAll();

            var category = SlotToCategory(slot);
            var allParts = PartMaster.Instance.GetDictionary();

            foreach (var kvp in allParts)
            {
                if (kvp.Value.CategoryType != category) continue;
                var btn = partsPool.Borrow();
                btn.SetData(kvp.Value.PartID, kvp.Value.name, OnSelectPart);
            }
        }

        private void OnSelectPart(int partId)
        {
            if (selectedSlot == PartsAttachType.None) return;
            editingDic[selectedSlot] = partId;
            ShowSlots();
        }

        private void OnClickSave()
        {
            var data = PartsAssembleData.CreateFromDictionary(editingDic);
            assembleDataAsset.Save(data);
            Debug.Log("[CustomizeManager] 機体データを保存しました。");
        }

        private void OnClickBack()
        {
            StateManager.Instance?.GoToState(returnState);
        }

        private static PartCategoryType SlotToCategory(PartsAttachType slot)
        {
            switch (slot)
            {
                case PartsAttachType.Head:         return PartCategoryType.Head;
                case PartsAttachType.Core:         return PartCategoryType.Core;
                case PartsAttachType.Arm:          return PartCategoryType.Arm;
                case PartsAttachType.Leg:          return PartCategoryType.Leg;
                case PartsAttachType.Booster:      return PartCategoryType.Booster;
                case PartsAttachType.Weapon_Right:
                case PartsAttachType.Weapon_Left:
                case PartsAttachType.Weapon_Sub:   return PartCategoryType.Gun;
                default:                           return PartCategoryType.None;
            }
        }
    }
}
