using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// ユニットの移動パターン。経過時間から位置と速度を式で求める（Unity のシーンに依存しない）。
    /// 座標はスポーンした位置を原点、スポーンしたときの向き（ヨー）を +Z とするローカル座標
    /// </summary>
    public interface IUnitMovePattern
    {
        /// <summary>経過時間 time（秒）での位置と速度</summary>
        void Evaluate(float time, out Vector3 localPosition, out Vector3 localVelocity);

        /// <summary>
        /// 向きを固定するか。true なら FixedForward に向け、false なら進行方向に向ける
        /// （縦の円運動は、進行方向の水平成分が半周ごとに反転するため向きを固定する）
        /// </summary>
        bool HasFixedForward { get; }

        /// <summary>向きを固定するときの正面（ローカル座標、水平）</summary>
        Vector3 FixedForward { get; }
    }
}
