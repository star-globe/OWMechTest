using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    /// <summary>
    /// 機体の見た目の部分（MechBody プレハブ）のルート。基準リグとパーツの取り付け先を持つ。
    /// バトルの機体（Player プレハブ）とカスタマイズ画面のプレビューで同じプレハブを使う
    /// </summary>
    public class MechBody : MonoBehaviour
    {
        [SerializeField]
        MechRig mechRig = null;

        [SerializeField]
        BoosterContainer boosterContainer = null;

        [SerializeField]
        GunAttach rightGunAttach = null;

        [SerializeField]
        GunAttach leftGunAttach = null;

        private void Awake()
        {
            Assert.IsNotNull(mechRig);
            Assert.IsNotNull(boosterContainer);
            Assert.IsNotNull(rightGunAttach);
            Assert.IsNotNull(leftGunAttach);
        }

        public MechRig Rig => mechRig;

        /// <summary>パーツを組み付ける。サブ武装は取り付け位置が無いので扱わない</summary>
        public MechPartsAssembler Assembler => new MechPartsAssembler(mechRig, boosterContainer, rightGunAttach, leftGunAttach, null);
    }
}
