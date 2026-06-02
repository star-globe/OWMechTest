namespace AdvancedGears
{
    /// <summary>部隊内での役割</summary>
    public enum SquadRole
    {
        None      = 0,
        Leader    = 1,   // リーダー：下位ユニットの移動目的地を指定する
        Vanguard  = 2,   // 前衛：積極的に接近・交戦
        Rearguard = 3,   // 後衛：距離を保って支援
        Scout     = 4,   // 偵察：広範囲を哨戒
        Support   = 5,   // 補給・修復
    }

    /// <summary>部隊全体への命令</summary>
    public enum SquadOrder
    {
        Idle    = 0,
        Advance = 1,
        Defend  = 2,
        Retreat = 3,
    }
}
