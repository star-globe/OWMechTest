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
        public const int HyperBoostShockwave = 5; // ハイパーブースト開始時の衝撃波

        // 戦闘系
        public const int MuzzleFlash  = 10; // 発砲マズルフラッシュ
        public const int HitSpark     = 11; // 被弾スパーク
        public const int Explosion    = 12; // 爆発（機体破壊・大爆発）
        public const int ExplosionSmall = 13; // 小爆発（パーツ破損）

        // ミサイル（#183 で EffectSettings を作る。作るまではミサイルの弾のマスタで設定せず、既存のエフェクトで代用する）
        public const int MissileLaunch   = 14; // ミサイルの発射（弾のマスタの muzzleEffectId）
        public const int MissileHit      = 15; // ミサイルの命中（弾のマスタの hitEffectId）
        public const int MissileShotDown = 16; // ミサイルが撃ち落とされた（弾のマスタの shootDownEffectId）

        // 環境・UI補助
        public const int LandingDust  = 20; // 着地砂煙
        public const int GroundBoostSpark = 21; // 接地ブースト中の足元の火花（機体に付けてループ再生する）
        public const int WallKick     = 22; // 壁を蹴ったときの粉じんと火花
    }
}
