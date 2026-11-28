using System;
using UnityEngine;

namespace AdvancedGears
{
    /// <summary>
    /// パーツの取り付け先（基準リグ・ブースター・銃とサブ装備の取り付け位置）をまとめ、スロットごとにパーツを組み付ける。
    /// バトルの機体（PlayerPartsContainer）とカスタマイズ画面のプレビュー（MechPreview）で共通に使う。
    /// 取り付け先が null のスロットは何もしない
    /// </summary>
    public readonly struct MechPartsAssembler
    {
        /// <summary>コアのパーツが付く胴体のボーン</summary>
        const string CoreBoneName = "Chest";

        readonly MechRig rig;
        readonly BoosterContainer booster;
        readonly GunAttach rightGun;
        readonly GunAttach leftGun;
        readonly SubPartAttach subPart;

        public MechPartsAssembler(MechRig rig, BoosterContainer booster, GunAttach rightGun, GunAttach leftGun, SubPartAttach subPart)
        {
            this.rig = rig;
            this.booster = booster;
            this.rightGun = rightGun;
            this.leftGun = leftGun;
            this.subPart = subPart;
        }

        /// <summary>見た目のあるすべてのスロットに組み付ける（ジェネレーターは見た目が無いので扱わない）</summary>
        /// <param name="getPartId">スロットのパーツ ID。未装備は負の値</param>
        public void AssembleAll(Func<PartsAttachType, int> getPartId)
        {
            foreach (var slot in MechFrameSlots.All)
                Assemble(slot, getPartId(slot));

            Assemble(PartsAttachType.Booster, getPartId(PartsAttachType.Booster));
            Assemble(PartsAttachType.Weapon_Right, getPartId(PartsAttachType.Weapon_Right));
            Assemble(PartsAttachType.Weapon_Left, getPartId(PartsAttachType.Weapon_Left));
            Assemble(PartsAttachType.Weapon_Sub, getPartId(PartsAttachType.Weapon_Sub));
        }

        /// <summary>1 つのスロットのパーツを差し替える。組み付け済みのパーツは外す</summary>
        public void Assemble(PartsAttachType slot, int partId)
        {
            if (MechFrameSlots.IsFrameSlot(slot))
            {
                AssembleFrame(slot, partId);
                return;
            }

            switch (slot)
            {
                case PartsAttachType.Booster:
                    if (booster != null)
                    {
                        FollowCore(booster.transform);
                        booster.SetBoosters(partId);
                    }
                    break;
                case PartsAttachType.Weapon_Right:
                    if (rightGun != null)
                        rightGun.SetGun(partId);
                    break;
                case PartsAttachType.Weapon_Left:
                    if (leftGun != null)
                        leftGun.SetGun(partId);
                    break;
                case PartsAttachType.Weapon_Sub:
                    if (subPart != null)
                    {
                        FollowCore(subPart.transform);
                        subPart.SetPart(partId);
                    }
                    break;
            }
        }

        /// <summary>
        /// コアに付く取り付け位置を胴体のボーンの子にして、アニメーションに追従させる。
        /// サブの取り付け位置はブースターの取り付け位置（Core）の子なので、先に Core が付け替わっていれば何もしない
        /// </summary>
        void FollowCore(Transform attach)
        {
            if (rig != null)
                rig.FollowBone(attach, CoreBoneName);
        }

        void AssembleFrame(PartsAttachType slot, int partId)
        {
            if (rig == null)
                return;

            var part = PartMaster.Instance.GetSettings(partId)?.InstantiatePart<MechFramePart>();
            if (part == null)
                Debug.LogErrorFormat("[MechPartsAssembler] {0} のパーツ（ID: {1}）を読み込めませんでした", slot, partId);

            rig.Attach(slot, part);
        }
    }
}
