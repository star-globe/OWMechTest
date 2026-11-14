namespace AdvancedGears
{
    /// <summary>
    /// 着地の判定。空中にいる間の最も速い落下速度を覚えておき、接地した瞬間に、
    /// その速さがしきい値以上なら着地とみなす（段差を降りただけのときは出さない）。
    /// 接地した瞬間の速度は衝突で 0 近くになっているため、空中の間の速度を使う
    /// </summary>
    public class LandingDetector
    {
        /// <summary>着地とみなす落下速度（m/s）。ジャンプの初速 8.5 m/s で跳んで降りると約 8.5 m/s、高さ 0.8 m 程度の段差では 4 m/s</summary>
        public const float DefaultMinFallSpeed = 4f;

        readonly float minFallSpeed;

        bool wasFloating;
        float maxFallSpeed;

        public LandingDetector(float minFallSpeed = DefaultMinFallSpeed)
        {
            this.minFallSpeed = minFallSpeed;
        }

        /// <summary>着地した時の落下速度（m/s）。Update が true を返したときに有効</summary>
        public float LastFallSpeed { get; private set; }

        /// <summary>毎フレーム呼ぶ。今のフレームで着地したら true</summary>
        public bool Update(bool isFloating, float velocityY)
        {
            if (isFloating)
            {
                if (!wasFloating)
                    maxFallSpeed = 0f;

                if (-velocityY > maxFallSpeed)
                    maxFallSpeed = -velocityY;

                wasFloating = true;
                return false;
            }

            var landed = wasFloating && maxFallSpeed >= minFallSpeed;
            if (landed)
                LastFallSpeed = maxFallSpeed;

            wasFloating = false;
            maxFallSpeed = 0f;
            return landed;
        }
    }
}
