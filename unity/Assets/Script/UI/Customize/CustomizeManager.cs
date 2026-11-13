using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        [Header("エネルギー")]
        [SerializeField] TextMeshProUGUI energyPerformanceText;
        [Tooltip("組めない理由と、保存データが組めなくなったときの警告")]
        [SerializeField] TextMeshProUGUI assembleMessageText;

        [Header("データ")]
        [SerializeField] PlayerPartsAssembleDataAsset assembleDataAsset;

        // 編集中の組み合わせ（スロット→パーツID）
        readonly Dictionary<PartsAttachType, int> editingDic = new Dictionary<PartsAttachType, int>();

        // 保存データがマスタの調整で組めなくなっているか（組める組み合わせを保存すると false に戻る）
        bool isSavedDataInvalid;

        PartsAttachType selectedSlot = PartsAttachType.None;

        static readonly PartsAttachType[] AllSlots = new[]
        {
            PartsAttachType.Head,
            PartsAttachType.Core,
            PartsAttachType.Arm,
            PartsAttachType.Leg,
            PartsAttachType.Generator,
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
            // 組めない保存データもそのまま編集できるようにし、警告だけ出す（Battle では初期の組み合わせを使う）
            isSavedDataInvalid = assembleDataAsset.Load() && !assembleDataAsset.IsAssemblable();
            if (isSavedDataInvalid)
                Debug.LogWarning("[CustomizeManager] 保存済みの組み合わせが組めない状態になっています。");

            editingDic.Clear();

            // 機体本体のスロットは、保存データに無ければ初期値のパーツを表示する（Battle と同じ組み合わせになる）
            foreach (var slot in AllSlots)
                editingDic[slot] = assembleDataAsset.GetEquippedPartId(slot);
        }

        private void ShowSlots()
        {
            slotPool.ReturnAll();
            foreach (var slot in AllSlots)
            {
                var btn = slotPool.Borrow();
                btn.SetData(slot, editingDic[slot], OnSelectSlot);
            }

            UpdateEnergyView();
        }

        /// <summary>
        /// エネルギーの性能と組めない理由を表示する。
        /// 組めない組み合わせ（ジェネレーターが無い、回復力が 0 以下）なら保存できなくする
        /// </summary>
        private void UpdateEnergyView()
        {
            var resolved = EnergyPerformanceResolver.TryResolve(GetEditingPartId, out var performance);

            if (energyPerformanceText != null)
            {
                energyPerformanceText.SetText(resolved
                    ? CustomizeEnergyText.FormatPerformance(performance, GetEditingGeneratorOutput())
                    : CustomizeEnergyText.FormatUnresolvedPerformance());
            }

            var messages = CustomizeEnergyText.GetInvalidReasons(resolved, performance);
            if (isSavedDataInvalid)
                messages.Add(CustomizeEnergyText.SavedDataInvalidWarning);

            if (assembleMessageText != null)
                assembleMessageText.SetText(string.Join("\n", messages));

            if (saveButton != null)
                saveButton.interactable = resolved && performance.IsValid;
        }

        private bool CanSave()
        {
            return EnergyPerformanceResolver.TryResolve(GetEditingPartId, out var performance) && performance.IsValid;
        }

        /// <summary>編集中のジェネレーターの総出力。TryResolve が true なら必ずマスタにある</summary>
        private int GetEditingGeneratorOutput()
        {
            return GeneratorMaster.Instance.GetDictionary().TryGetValue(GetEditingPartId(PartsAttachType.Generator), out var generator) && generator != null
                ? generator.EnergyOutput
                : 0;
        }

        private int GetEditingPartId(PartsAttachType slot)
        {
            return editingDic.TryGetValue(slot, out var partId) ? partId : -1;
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
                btn.SetData(kvp.Value, OnSelectPart);
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
            if (!CanSave())
            {
                Debug.LogWarning("[CustomizeManager] 組めない組み合わせのため保存しません。");
                return;
            }

            var data = PartsAssembleData.CreateFromDictionary(editingDic);
            assembleDataAsset.Save(data);
            Debug.Log("[CustomizeManager] 機体データを保存しました。");

            isSavedDataInvalid = false;
            UpdateEnergyView();
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
                case PartsAttachType.Generator:    return PartCategoryType.Generator;
                case PartsAttachType.Booster:      return PartCategoryType.Booster;
                case PartsAttachType.Weapon_Right:
                case PartsAttachType.Weapon_Left:
                case PartsAttachType.Weapon_Sub:   return PartCategoryType.Gun;
                default:                           return PartCategoryType.None;
            }
        }
    }
}
