using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクト確認シーンのブースター。台に付けたブースターに、ゲーム内と同じ呼び方で
    /// 通常の噴射（毎フレーム Boost）・クイックブースト（押した瞬間に Quick）・ハイパーブースト（毎フレーム HyperBoost）をさせ、
    /// 基本色をパーツの色とプリセットで切り替える。出力の強弱は、当面この 3 つの状態の切り替えで確かめる
    /// </summary>
    public class BoosterEffectTester : PartEffectTester
    {
        [Header("操作")]
        [SerializeField] Button boostButton;
        [SerializeField] Button quickButton;
        [SerializeField] Button hyperButton;
        [SerializeField] Button colorButton;

        [Header("基本色")]
        [Tooltip("パーツの色の次に順に切り替える基本色（HDR）")]
        [SerializeField, ColorUsage(false, true)]
        Color[] colorPresets = new[]
        {
            BoosterColorPalette.DefaultBaseColor,
            new Color(2.0f, 0.6f, 0.2f, 1f),
            new Color(0.4f, 2.0f, 0.6f, 1f),
            new Color(2.0f, 0.3f, 1.6f, 1f),
        };

        BoosterEffectCheckState state;
        BoosterPart booster;

        protected override void Awake()
        {
            base.Awake();

            state = new BoosterEffectCheckState(colorPresets != null ? colorPresets.Length : 0);

            boostButton?.onClick.AddListener(OnClickBoost);
            quickButton?.onClick.AddListener(OnClickQuick);
            hyperButton?.onClick.AddListener(OnClickHyper);
            colorButton?.onClick.AddListener(OnClickColor);
        }

        protected override IEnumerable<PartSettings> GetCandidates()
        {
            var list = new List<PartSettings>(BoosterMaster.Instance.GetDictionary().Values);
            list.Sort((a, b) => a.PartID.CompareTo(b.PartID));
            return list;
        }

        protected override void OnSpawned(PartComponent part)
        {
            booster = part as BoosterPart;
            if (booster == null)
            {
                Debug.LogErrorFormat(this, "[BoosterEffectTester] {0} は BoosterPart ではありません", part.name);
                return;
            }

            // 新しいパーツは噴射していない状態で始まるので、操作の状態もそろえる
            state.ResetBoost();
            booster.SetBoostVector(BoostVector.Forward);
            ApplyColor();
        }

        private void Update()
        {
            if (booster == null)
                return;

            booster.Boost(state.BoostBit);
            booster.HyperBoost(state.HyperBit);
        }

        private void OnClickBoost()
        {
            state.ToggleBoost();
            UpdateStatus();
        }

        private void OnClickQuick()
        {
            booster?.Quick(state.QuickBit);
        }

        private void OnClickHyper()
        {
            state.ToggleHyper();
            UpdateStatus();
        }

        private void OnClickColor()
        {
            state.NextColor();
            ApplyColor();
            UpdateStatus();
        }

        /// <summary>BoosterContainer と同じく、上書きの色 → パーツの色 → 既定の色 の順で基本色を決める</summary>
        private void ApplyColor()
        {
            if (booster == null)
                return;

            var sources = new List<IBoosterColorSource>();
            if (!state.IsPartColor)
                sources.Add(new FixedBoosterColorSource(colorPresets[state.ColorIndex]));
            sources.Add(new BoosterPartColorSource(BoosterMaster.Instance.GetSettings(CurrentPartId)));

            booster.SetBaseColor(BoosterColorResolver.Resolve(sources, BoosterColorPalette.DefaultBaseColor));
        }

        protected override string GetStatus()
        {
            if (state == null)
                return string.Empty;

            var color = state.IsPartColor ? "パーツの色" : $"プリセット {state.ColorIndex + 1}";
            return $"ブースト: {(state.IsBoostOn ? "ON" : "OFF")}　ハイパー: {(state.IsHyperOn ? "ON" : "OFF")}　基本色: {color}";
        }

        /// <summary>プリセットの色を、内装などと同じ「パーツの色より優先する供給元」として渡す</summary>
        class FixedBoosterColorSource : IBoosterColorSource
        {
            readonly Color color;

            public FixedBoosterColorSource(Color color)
            {
                this.color = color;
            }

            public bool TryGetBaseColor(out Color baseColor)
            {
                baseColor = color;
                return true;
            }
        }
    }
}
