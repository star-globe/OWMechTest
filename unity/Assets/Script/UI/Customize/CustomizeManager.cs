using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    /// <summary>
    /// カスタマイズ画面の全体制御。
    /// スロット選択パネルとパーツ選択パネルを管理し、変更を PlayerPrefs へ保存する。
    /// MainScene を通らずに起動したときはデバッグモードになり、保存データを読み書きしない（CustomizeDebugMode）。
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

        /// <summary>編集中の組み合わせが変わったとき（最初の読み込みを含む）に呼ぶ。プレビューの組み直しに使う</summary>
        public event Action<IReadOnlyDictionary<PartsAttachType, int>> AssemblyChanged;

        // MainScene の初期化を通らずに起動したか（CustomizeDebugMode 参照）
        bool isDebugMode;

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

        private void Awake()
        {
            // MainScene を通らずに Play したときはデバッグモードにし、必要な初期化（マスタの読み込み）をここで行う。
            // 他のコンポーネントの Start がマスタを使えるよう、Start より前（Awake）に読み込む
            isDebugMode = CustomizeDebugMode.IsDebugMode(DebugBootstrap.IsInitialized);
            if (isDebugMode)
            {
                Debug.Log("[CustomizeManager] MainScene を通らずに起動したため、デバッグモードで開きます。");
                DebugBootstrap.EnsureInitialized();
            }
        }

        private void Start()
        {
            slotPool.Initialize();
            partsPool.Initialize();

            saveButton?.onClick.AddListener(OnClickSave);
            backButton?.onClick.AddListener(OnClickBack);

            if (backButton != null)
                backButton.interactable = CustomizeDebugMode.CanGoBack(isDebugMode);

            LoadCurrentData();
            ShowSlots();
            NotifyAssemblyChanged();
        }

        private void LoadCurrentData()
        {
            // デバッグモードでは保存データ（PlayerPrefs）を読まず、アセットの初期値から始める
            if (isDebugMode)
            {
                isSavedDataInvalid = false;
            }
            else
            {
                // 組めない保存データもそのまま編集できるようにし、警告だけ出す（Battle では初期の組み合わせを使う）
                isSavedDataInvalid = assembleDataAsset.Load() && !assembleDataAsset.IsAssemblable();
                if (isSavedDataInvalid)
                    Debug.LogWarning("[CustomizeManager] 保存済みの組み合わせが組めない状態になっています。");
            }

            editingDic.Clear();

            // 通常は、機体本体のスロットが保存データに無ければ初期値のパーツを表示する（Battle と同じ組み合わせになる）
            var initial = CustomizeDebugMode.BuildInitialAssembly(AllSlots, isDebugMode,
                                                                  assembleDataAsset.GetEquippedPartId,
                                                                  assembleDataAsset.GetDefaultPartId);
            foreach (var kvp in initial)
                editingDic[kvp.Key] = kvp.Value;
        }

        private void NotifyAssemblyChanged()
        {
            AssemblyChanged?.Invoke(editingDic);
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

            var mobilityResolved = MobilityPerformanceResolver.TryResolve(GetEditingPartId, PlayerControllerConst.Instance.Mobility, out var mobility);

            if (energyPerformanceText != null)
            {
                var energyText = resolved
                    ? CustomizeEnergyText.FormatPerformance(performance, GetEditingGeneratorOutput())
                    : CustomizeEnergyText.FormatUnresolvedPerformance();
                var mobilityText = mobilityResolved
                    ? CustomizeMobilityText.FormatPerformance(mobility, GetEditingBoostSpeedRate())
                    : CustomizeMobilityText.UnresolvedText;
                energyPerformanceText.SetText(energyText + "\n\n" + mobilityText);
            }

            // デバッグモードでも組めない組み合わせは組み立てられるようにし、理由を警告として出すだけにする
            var messages = CustomizeEnergyText.GetInvalidReasons(resolved, performance);

            // 積載量の超過は組めない理由ではない（遅くなるだけ）ので、保存の可否には使わない
            var overweightWarning = mobilityResolved ? CustomizeMobilityText.GetOverweightWarning(mobility) : null;
            if (overweightWarning != null)
                messages.Add(overweightWarning);
            var invalidSlots = FindInvalidSlots();
            foreach (var slot in invalidSlots)
                messages.Add(CustomizeEnergyText.FormatInvalidSlotReason(PartsSlotButton.SlotDisplayName(slot)));
            if (isSavedDataInvalid)
                messages.Add(CustomizeEnergyText.SavedDataInvalidWarning);
            if (isDebugMode)
                messages.Insert(0, CustomizeDebugMode.Notice);

            if (assembleMessageText != null)
                assembleMessageText.SetText(string.Join("\n", messages));

            if (saveButton != null)
                saveButton.interactable = CustomizeDebugMode.CanSave(isDebugMode, resolved && performance.IsValid && invalidSlots.Count == 0);
        }

        private bool CanSave()
        {
            var isAssemblable = EnergyPerformanceResolver.TryResolve(GetEditingPartId, out var performance) && performance.IsValid
                                && FindInvalidSlots().Count == 0;
            return CustomizeDebugMode.CanSave(isDebugMode, isAssemblable);
        }

        /// <summary>編集中の組み合わせで、付けられないカテゴリのパーツが入っているスロット</summary>
        private List<PartsAttachType> FindInvalidSlots()
        {
            return PartSlotRule.FindInvalidSlots(PartSlotRule.WeaponSlots, GetEditingPartId, PartSlotRule.GetMasterCategory);
        }

        /// <summary>編集中のジェネレーターの総出力。TryResolve が true なら必ずマスタにある</summary>
        private int GetEditingGeneratorOutput()
        {
            return GeneratorMaster.Instance.GetDictionary().TryGetValue(GetEditingPartId(PartsAttachType.Generator), out var generator) && generator != null
                ? generator.EnergyOutput
                : 0;
        }

        /// <summary>編集中のブースターのブースト出力。ブースターが無い・マスタに無ければ null</summary>
        private float? GetEditingBoostSpeedRate()
        {
            var boosterId = GetEditingPartId(PartsAttachType.Booster);
            return boosterId >= 0 && BoosterMaster.Instance.GetDictionary().TryGetValue(boosterId, out var booster) && booster != null
                ? booster.BoostSpeedRate
                : (float?) null;
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

            var allParts = PartMaster.Instance.GetDictionary();

            foreach (var kvp in allParts)
            {
                if (!PartSlotRule.CanEquip(slot, kvp.Value.CategoryType)) continue;
                // パーツの保持情報（どのパーツを持っているか）を追加したら、ここで持っていないパーツを除く。
                // ただしデバッグモードでは保持情報を使わず、マスタにある全パーツを選べるようにする
                var btn = partsPool.Borrow();
                btn.SetData(kvp.Value, OnSelectPart);
            }
        }

        private void OnSelectPart(int partId)
        {
            if (selectedSlot == PartsAttachType.None) return;
            editingDic[selectedSlot] = partId;
            ShowSlots();
            NotifyAssemblyChanged();
        }

        private void OnClickSave()
        {
            if (!CanSave())
            {
                Debug.LogWarning(isDebugMode
                    ? "[CustomizeManager] デバッグモードのため保存しません。"
                    : "[CustomizeManager] 組めない組み合わせのため保存しません。");
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
            if (!CustomizeDebugMode.CanGoBack(isDebugMode))
                return;

            StateManager.Instance?.GoToState(returnState);
        }
    }
}
