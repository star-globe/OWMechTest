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
        }

        private void Start()
        {
            // プレイヤー機体はカスタマイズ画面で保存した組み合わせを反映する（保存データが無ければアセットの初期値）
            if (assembleDataAsset is PlayerPartsAssembleDataAsset playerAsset)
                playerAsset.Load();

            Load();
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

        public void RightFire(Vector3 tgt)
        {
            if (rightGunAttach != null)
                rightGunAttach.Fire(tgt);

            if (mechRig != null)
                mechRig.NotifyFire(isRight: true);
        }

        public void LeftFire(Vector3 tgt)
        {
            if (leftGunAttach != null)
                leftGunAttach.Fire(tgt);

            if (mechRig != null)
                mechRig.NotifyFire(isRight: false);
        }

        public void SubFire(Vector3 tgt)
        {
            if (subGunAttach != null)
                subGunAttach.Fire(tgt);
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
