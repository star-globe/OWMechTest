namespace AdvancedGears
{
    /// <summary>
    /// 移動の状態に合わせて、ブースターのエフェクト（PlayerPartsContainer）を切り替える。
    /// 着地の砂煙など、足元に出すエフェクトも再生する
    /// </summary>
    public class PlayerEffectPresenter
    {
        readonly PlayerPartsContainer partsContainer;
        readonly LandingDetector landingDetector = new LandingDetector();

        public PlayerEffectPresenter(PlayerPartsContainer partsContainer)
        {
            this.partsContainer = partsContainer;
        }

        /// <summary>
        /// 通常ブーストのビット。上昇中は全方向（継続的なブースト）、空中では前後、地上では入力方向。ブーストしていなければ 0
        /// </summary>
        public static int CalcBoostBit(bool isBoost, bool isFloating, bool isAscend, float inputX, float inputZ)
        {
            if (!isBoost)
                return 0;

            if (isAscend)
                return ActionUtils.AscendBit;

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

        public void Apply(bool isBoost, bool isFloating, bool isAscend, float inputX, float inputZ,
                          in JumpInfo jumpInfo, in JumpInfo quickInfo, in HyperBoostInfo hyperBoostInfo)
        {
            // 上昇は継続的なブースト（通常ブーストのエフェクト）、ジャンプは瞬発的なブースト（クイックのエフェクト）で描画する
            partsContainer.Boost(CalcBoostBit(isBoost, isFloating, isAscend, inputX, inputZ));

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

        /// <summary>着地した瞬間に、足元へ砂煙を出す</summary>
        public void ApplyLanding(bool isFloating, float velocityY, UnityEngine.Vector3 footPosition)
        {
            if (landingDetector.Update(isFloating, velocityY))
                EffectManager.Instance?.PlayEffect(EffectID.LandingDust, footPosition);
        }

        /// <summary>足元の火花が出始める水平速度（m/s）。歩きの最高速度 9 m/s より下から、ブーストで加速し始めたら出す</summary>
        public const float GroundSparkMinSpeed = 4f;

        /// <summary>足元の火花が最も多くなる水平速度（m/s）。地上ブーストの最高速度（歩きの 2 倍の 18 m/s）</summary>
        public const float GroundSparkFullSpeed = 18f;

        /// <summary>足元の火花の最大の発生数（個/秒）</summary>
        public const float GroundSparkMaxRate = 60f;

        /// <summary>
        /// 足元の火花の発生数（個/秒）。地上でブーストしている間だけ、水平速度が
        /// GroundSparkMinSpeed から GroundSparkFullSpeed になるまでの間で 0 から最大まで増やす
        /// </summary>
        public static float CalcGroundSparkRate(bool isBoost, bool isFloating, float horizontalSpeed)
        {
            if (!isBoost || isFloating)
                return 0f;

            var t = UnityEngine.Mathf.InverseLerp(GroundSparkMinSpeed, GroundSparkFullSpeed, horizontalSpeed);
            return t * GroundSparkMaxRate;
        }

        UnityEngine.ParticleSystem groundSpark;

        /// <summary>足元の火花（機体に付けたループの ParticleSystem）を設定する</summary>
        public void SetGroundSpark(UnityEngine.ParticleSystem particle)
        {
            groundSpark = particle;
            if (groundSpark == null)
                return;

            var emission = groundSpark.emission;
            emission.rateOverTime = 0f;
            groundSpark.Play();
        }

        /// <summary>地上でブースト移動している間、足元から火花を出す</summary>
        public void ApplyGroundSpark(bool isBoost, bool isFloating, float horizontalSpeed)
        {
            if (groundSpark == null)
                return;

            var emission = groundSpark.emission;
            emission.rateOverTime = CalcGroundSparkRate(isBoost, isFloating, horizontalSpeed);
        }
    }
}
