using System;

namespace AdvancedGears
{
    /// <summary>
    /// エフェクト確認シーンでのブースターの操作状態（噴射の ON / OFF、基本色の選び方）。
    /// ブースターに渡す噴射方向のビットもここで求める。Unity に依存しないのでテストできる
    /// </summary>
    public class BoosterEffectCheckState
    {
        /// <summary>基本色にパーツ（BoosterSettings）の色を使うときの ColorIndex</summary>
        public const int PartColorIndex = -1;

        /// <summary>
        /// すべての噴射方向のビット。台に付けるブースターは 1 つなので、向き（BoostVector）にかかわらず噴射させる
        /// </summary>
        public static readonly int AllDirectionsBit = CalcAllDirectionsBit();

        readonly int presetCount;

        /// <summary>通常の噴射（ブースト）が ON か。ゲーム内ではブースト中に毎フレーム Boost を呼ぶ</summary>
        public bool IsBoostOn { get; private set; }

        /// <summary>ハイパーブーストが ON か。ゲーム内では発動中に毎フレーム HyperBoost を呼ぶ</summary>
        public bool IsHyperOn { get; private set; }

        /// <summary>基本色の選択。PartColorIndex ならパーツの色、0 以上なら色のプリセットの番号</summary>
        public int ColorIndex { get; private set; } = PartColorIndex;

        /// <param name="presetCount">基本色のプリセットの数（パーツの色は含めない）</param>
        public BoosterEffectCheckState(int presetCount)
        {
            this.presetCount = Math.Max(0, presetCount);
        }

        /// <summary>毎フレーム Boost に渡すビット</summary>
        public int BoostBit => IsBoostOn ? AllDirectionsBit : 0;

        /// <summary>毎フレーム HyperBoost に渡すビット</summary>
        public int HyperBit => IsHyperOn ? AllDirectionsBit : 0;

        /// <summary>クイックブーストのボタンを押したときに Quick に渡すビット（ゲーム内では発動した最初のフレームだけ呼ぶ）</summary>
        public int QuickBit => AllDirectionsBit;

        public bool IsPartColor => ColorIndex == PartColorIndex;

        public void ToggleBoost() => IsBoostOn = !IsBoostOn;

        public void ToggleHyper() => IsHyperOn = !IsHyperOn;

        /// <summary>パーツを差し替えたときに呼ぶ。噴射は止め、色の選択は残す</summary>
        public void ResetBoost()
        {
            IsBoostOn = false;
            IsHyperOn = false;
        }

        /// <summary>基本色をパーツの色 → プリセット 0 → … → 最後のプリセット → パーツの色 の順に切り替える</summary>
        public void NextColor()
        {
            ColorIndex = ColorIndex + 1 >= presetCount ? PartColorIndex : ColorIndex + 1;
        }

        static int CalcAllDirectionsBit()
        {
            var bit = 0;
            foreach (BoostVector vec in Enum.GetValues(typeof(BoostVector)))
                bit |= 1 << (byte)vec;
            return bit;
        }
    }
}
