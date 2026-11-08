namespace AdvancedGears
{
    /// <summary>
    /// 移動の状態に合わせて、ブースターのエフェクト（PlayerPartsContainer）を切り替える
    /// </summary>
    public class PlayerEffectPresenter
    {
        readonly PlayerPartsContainer partsContainer;

        public PlayerEffectPresenter(PlayerPartsContainer partsContainer)
        {
            this.partsContainer = partsContainer;
        }

        /// <summary>通常ブーストのビット。空中では前後、地上では入力方向。ブーストしていなければ 0</summary>
        public static int CalcBoostBit(bool isBoost, bool isFloating, float inputX, float inputZ)
        {
            if (!isBoost)
                return 0;

            if (isFloating)
                return ActionUtils.FloatingBit;

            return ActionUtils.ConvertBitXZ(inputX, inputZ);
        }

        /// <summary>ジャンプ・クイックを開始した最初のフレームか</summary>
        public static bool IsImpulseStarted(in JumpInfo info)
        {
            return info.isJump && info.jumpSumTime == 0;
        }

        public static int CalcHyperBoostBit(in HyperBoostInfo info)
        {
            return info.isOn ? ActionUtils.ConvertBit(info.localBoostVector) : 0;
        }

        public void Apply(bool isBoost, bool isFloating, float inputX, float inputZ,
                          in JumpInfo jumpInfo, in JumpInfo quickInfo, in HyperBoostInfo hyperBoostInfo)
        {
            partsContainer.Boost(CalcBoostBit(isBoost, isFloating, inputX, inputZ));

            if (IsImpulseStarted(quickInfo))
            {
                partsContainer.Quick(ActionUtils.ConvertBit(quickInfo.localNor));
            }

            if (IsImpulseStarted(jumpInfo))
            {
                partsContainer.Quick(ActionUtils.ConvertBit(jumpInfo.localNor));
            }

            partsContainer.HyperBoost(CalcHyperBoostBit(hyperBoostInfo));
        }
    }
}
