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
                prefabObject = this.gun.gameObject;
            }
            else
            {
                this.gun = null;
            }

            connector.Exchange(prefabObject);
        }

        public void Fire(Vector3 target)
        {
            if (gun != null)
                gun.Fire(target);
        }
    }
}
