using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// 機体本体のパーツ（頭部・胴体・腕・脚）。共通スケルトンでスキニングした FBX のプレハブに付ける。
    /// 装着時に MechRig が SkinnedMeshRenderer を基準リグのボーンに付け替えるので、パーツ側は Animator を持たない
    /// </summary>
    public class MechFramePart : PartComponent
    {
        public SkinnedMeshRenderer[] GetRenderers()
        {
            return GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }
    }
}
