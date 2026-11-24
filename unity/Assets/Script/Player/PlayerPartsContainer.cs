using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("subGunAttach")]
        SubPartAttach subPartAttach = null;

        [SerializeField]
        MechRig mechRig = null;

        [SerializeField]
        PartsAssembleDataAsset assembleDataAsset;

        private void Awake()
        {
            Assert.IsNotNull(assembleDataAsset);

            // プレイヤー機体はカスタマイズ画面で保存した組み合わせを反映する（保存データが無ければアセットの初期値）。
            // PlayerController.Start がエネルギーの性能を計算するので、Start より前（Awake）に読み込む
            if (assembleDataAsset is PlayerPartsAssembleDataAsset playerAsset && playerAsset.Load())
            {
                // マスタの調整で保存データが組めなくなっていたら、初期の組み合わせで出撃する（保存データは消さない）
                if (!playerAsset.IsAssemblable())
                {
                    Debug.LogWarningFormat(this, "[PlayerPartsContainer] {0}: 保存済みの組み合わせが組めないため、初期の組み合わせを使います", name);
                    playerAsset.UseDefaultData();
                }
            }
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
            // 機体本体は未設定なら初期値を使い、それ以外のスロットは未設定なら外す（GetEquippedPartId）
            new MechPartsAssembler(mechRig, boosterContainer, rightGunAttach, leftGunAttach, subPartAttach)
                .AssembleAll(assembleDataAsset.GetEquippedPartId);
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
                _                => null,
            };
        }

        /// <summary>その武器で二次ロックになるまでの時間（秒）。武器が無ければ二次ロックにならない</summary>
        public float GetSecondaryLockTime(WeaponSlot slot)
        {
            if (slot == WeaponSlot.Sub)
                return subPartAttach != null ? subPartAttach.SecondaryLockTime : SecondaryLockTime.Never;

            var attach = GetGunAttach(slot);
            return attach != null ? attach.SecondaryLockTime : SecondaryLockTime.Never;
        }

        /// <summary>腕の武器で撃つ、またはサブ装備を使う。腕の武器だけ射撃アニメーションを出す</summary>
        public void Fire(WeaponSlot slot, in AimRequest request)
        {
            if (slot == WeaponSlot.Sub)
            {
                if (subPartAttach != null)
                    subPartAttach.Use(request);
                return;
            }

            var attach = GetGunAttach(slot);
            if (attach != null)
                attach.Fire(request);

            if (mechRig != null)
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
