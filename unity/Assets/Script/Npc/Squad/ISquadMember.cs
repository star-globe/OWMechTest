using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 諸兵科連合に参加できる移動可能ユニットのインターフェース。
    /// 固定ユニット（砲台など）は実装しない。
    /// </summary>
    public interface ISquadMember
    {
        SquadRole Role { get; }

        /// <summary>個々のユニット設定値としての基本探知距離</summary>
        float DefaultRadius { get; }

        /// <summary>移動目的地とゴール判定距離を設定する</summary>
        void SetDestination(Vector3 destination, float goalDistance);

        /// <summary>移動目的地をクリアし、自律行動に戻す</summary>
        void ClearDestination();

        /// <summary>この部隊メンバーが属する SquadData を設定する</summary>
        void SetSquad(SquadData squad, SquadRole role);
    }
}
