using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UniRx;

namespace AdvancedGears
{
    public class PlayerPartsContainer : MonoBehaviour
    {
        [SerializeField]
        BoosterContainer boosterContainer = null;

        [SerializeField]
        GunAttach rightGunAttach = null;

        [SerializeField]
        GunAttach leftGunAttach = null;

        [SerializeField]
        GunAttach subGunAttach = null;

        [SerializeField]
        MechRig mechRig = null;

        [SerializeField]
        PartsAssembleDataAsset assembleDataAsset;

        private void Awake()
        {
            Assert.IsNotNull(assembleDataAsset);

            // プレイヤー機体はカスタマイズ画面で保存した組み合わせを反映する（保存データが無ければアセットの初期値）。
            // PlayerController.Start がエネルギーの性能を計算するので、Start より前（Awake）に読み込む
            if (assembleDataAsset is PlayerPartsAssembleDataAsset playerAsset)
                playerAsset.Load();
        }

        private void Start()
        {
            Load();
        }

        /// <summary>装備からエネルギーの性能を計算する。ジェネレーターを読み込めなければ false</summary>
        public bool TryGetEnergyPerformance(out EnergyPerformance performance)
        {
            return EnergyPerformanceResolver.TryResolve(assembleDataAsset.GetEquippedPartId, out performance);
        }

        private void Load()
        {
            if (mechRig != null)
                LoadFrame();

            if (boosterContainer != null)
                boosterContainer.SetBoosters(assembleDataAsset.GetPartId(PartsAttachType.Booster));

            if (rightGunAttach != null)
                rightGunAttach.SetGun(assembleDataAsset.GetPartId(PartsAttachType.Weapon_Right));

            if (leftGunAttach != null)
                leftGunAttach.SetGun(assembleDataAsset.GetPartId(PartsAttachType.Weapon_Left));

            if (subGunAttach != null)
                subGunAttach.SetGun(assembleDataAsset.GetPartId(PartsAttachType.Weapon_Sub));
        }

        private void LoadFrame()
        {
            foreach (var slot in MechFrameSlots.All)
            {
                var partId = assembleDataAsset.GetEquippedPartId(slot);
                var part = PartMaster.Instance.GetSettings(partId)?.InstantiatePart<MechFramePart>();
                if (part == null)
                    Debug.LogErrorFormat("[PlayerPartsContainer] {0} のパーツ（ID: {1}）を読み込めませんでした", slot, partId);

                mechRig.Attach(slot, part);
            }
        }

        /// <summary>移動の状態をアニメーションに反映する</summary>
        public void SetMove(float horizontalSpeed, bool isGrounded)
        {
            if (mechRig != null)
                mechRig.SetMove(horizontalSpeed, isGrounded);
        }

        public void Boost(int vectorBit)
        {
            if (boosterContainer != null)
                boosterContainer.Boost(vectorBit);
        }

        GunAttach GetGunAttach(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Right => rightGunAttach,
                WeaponSlot.Left  => leftGunAttach,
                WeaponSlot.Sub   => subGunAttach,
                _                => null,
            };
        }

        /// <summary>その武器で二次ロックになるまでの時間（秒）。武器が無ければ二次ロックにならない</summary>
        public float GetSecondaryLockTime(WeaponSlot slot)
        {
            var attach = GetGunAttach(slot);
            return attach != null ? attach.SecondaryLockTime : SecondaryLockTime.Never;
        }

        public void Fire(WeaponSlot slot, in AimRequest request)
        {
            var attach = GetGunAttach(slot);
            if (attach != null)
                attach.Fire(request);

            if (mechRig != null && slot != WeaponSlot.Sub)
                mechRig.NotifyFire(isRight: slot == WeaponSlot.Right);
        }

        public void Quick(int vectorBit)
        {
            if (boosterContainer != null)
                boosterContainer.Quick(vectorBit);
        }

        public void HyperBoost(int vectorBit)
        {
            if (boosterContainer != null)
                boosterContainer.HyperBoost(vectorBit);
        }
    }
}
