namespace AdvancedGears
{
    /// <summary>
    /// ゲーム内で使用するエフェクトのID定数。
    /// EffectSettings アセットの effectId と一致させてください。
    /// </summary>
    public static class EffectID
    {
        // 推進・移動系
        public const int Thruster     = 1;  // 通常ブースター炎
        public const int ThrusterQuick = 2; // クイックブースト炎
        public const int ThrusterHyper = 3; // ハイパーブースト炎
        public const int Jump          = 4; // ジャンプ噴射

        // 戦闘系
        public const int MuzzleFlash  = 10; // 発砲マズルフラッシュ
        public const int HitSpark     = 11; // 被弾スパーク
        public const int Explosion    = 12; // 爆発（機体破壊・大爆発）
        public const int ExplosionSmall = 13; // 小爆発（パーツ破損）

        // 環境・UI補助
        public const int LandingDust  = 20; // 着地砂煙
    }
}
