using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class GunAttach : MonoBehaviour
    {
        [SerializeField]
        PartConnector connector;

        [SerializeField]
        [Tooltip("この取り付け位置で銃を持つ側（左右で鏡像のモデルを使い分ける）")]
        GunSide side = GunSide.Right;

        GunPart gun = null;

        private void Awake()
        {
            Assert.IsNotNull(connector);
        }

        public void SetGun(int gunId)
        {
            GameObject prefabObject = null;

            var instGun = GunMaster.InstantiateGunPart(gunId);
            if (instGun != null)
            {
                this.gun = instGun;
                this.gun.SetSide(side);
                prefabObject = this.gun.gameObject;
            }
            else
            {
                this.gun = null;
            }

            connector.Exchange(prefabObject);
        }

        /// <summary>二次ロックになるまでの時間（秒）。銃が無ければ二次ロックにならない</summary>
        public float SecondaryLockTime => gun != null ? gun.SecondaryLockTime : AdvancedGears.SecondaryLockTime.Never;

        public void Fire(in AimRequest request)
        {
            if (gun != null)
                gun.Fire(request);
        }
    }
}
