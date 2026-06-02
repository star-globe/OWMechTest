namespace AdvancedGears
{
    /// <summary>部隊内での役割（兵科）</summary>
    public enum SquadRole
    {
        None      = 0,
        Vanguard  = 1,   // 前衛：積極的に接近・交戦
        Rearguard = 2,   // 後衛：遠距離から支援射撃
        Scout     = 3,   // 偵察：広範囲を哨戒し目標を探す
        Support   = 4,   // 補給・修復：同じ部隊の近くに待機
    }

    /// <summary>部隊に対して発令できる命令</summary>
    public enum SquadOrder
    {
        Idle    = 0,   // 待機
        Advance = 1,   // 前進・攻撃
        Defend  = 2,   // 現位置防衛
        Retreat = 3,   // 撤退
    }
}
