namespace AdvancedGears
{
    /// <summary>攻撃の属性。被弾側の属性ごとの装甲と組み合わせてダメージを決める</summary>
    public enum AttackAttribute
    {
        /// <summary>実弾（既定値。マスタで未設定の弾はこれになる）</summary>
        Physical = 0,
        /// <summary>EN</summary>
        Energy = 1,
    }

    /// <summary>
    /// 被弾側へ渡す攻撃の情報（攻撃力と属性）。
    /// 弾に限らず、爆発など弾以外の攻撃もこれを作って HitReceiver.Damage に渡す
    /// </summary>
    public readonly struct AttackInfo
    {
        public readonly int Power;
        public readonly AttackAttribute Attribute;

        public AttackInfo(int power, AttackAttribute attribute)
        {
            Power = power;
            Attribute = attribute;
        }

        public override string ToString() => $"{Attribute}:{Power}";
    }
}
