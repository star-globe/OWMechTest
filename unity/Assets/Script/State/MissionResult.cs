namespace AdvancedGears
{
    /// <summary>任務の結果</summary>
    public enum MissionResult
    {
        None    = 0,   // 戦闘中（まだ決まっていない）
        Success = 1,   // 目標を撃破した
        Failed  = 2,   // プレイヤーの機体が撃破された
    }

    /// <summary>
    /// 戦闘 1 回分の任務の結果。最初に決まった結果だけを残す
    /// （自機の撃破の演出中に目標を壊しても、結果は失敗のまま）
    /// </summary>
    public class MissionResultHolder
    {
        public MissionResult Result { get; private set; } = MissionResult.None;

        public bool IsDecided => Result != MissionResult.None;

        /// <summary>戦闘の開始時に呼ぶ</summary>
        public void Reset()
        {
            Result = MissionResult.None;
        }

        /// <summary>結果を決める。すでに決まっていれば何もせず false を返す</summary>
        public bool TryDecide(MissionResult result)
        {
            if (IsDecided || result == MissionResult.None)
                return false;

            Result = result;
            return true;
        }
    }
}
