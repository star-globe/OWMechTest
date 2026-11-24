namespace AdvancedGears
{
    /// <summary>
    /// サブ（肩）に付けるサブ装備。サブ射撃ボタンで Use を呼ぶ。
    /// 射出するもの（ミサイル。GunPart が実装する）と、射出しないもの（一時的に性能を上げる装備など）がある
    /// </summary>
    public interface ISubPart
    {
        /// <summary>サブ装備を使う（射出する装備は撃ち、射出しない装備は効果を発動する）</summary>
        void Use(in AimRequest request);

        /// <summary>二次ロックになるまでの時間（秒）。ロックを使わない装備は SecondaryLockTime.Never</summary>
        float SecondaryLockTime { get; }
    }
}
