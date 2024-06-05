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
        PartsAssembleDataAsset assembleDataAsset;

        private void Awake()
        {
            Assert.IsNotNull(assembleDataAsset);
        }

        private void Start()
        {
            Load();
        }

        private void Load()
        {
            if (boosterContainer != null)
                boosterContainer.SetBoosters(assembleDataAsset.GetPartId(PartsAttachType.Booster));

            if (rightGunAttach != null)
                rightGunAttach.SetGun(assembleDataAsset.GetPartId(PartsAttachType.Weapon_Right));

            if (leftGunAttach != null)
                leftGunAttach.SetGun(assembleDataAsset.GetPartId(PartsAttachType.Weapon_Left));
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
        }

        public void LeftFire(Vector3 tgt)
        {
            if (leftGunAttach != null)
                leftGunAttach.Fire(tgt);
        }

        public void Quick(int vectorBit)
        {
            if (boosterContainer != null)
                boosterContainer.Quick(vectorBit);
        }
    }
}
