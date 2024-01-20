using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PartConnector : MonoBehaviour
    {
        GameObject partObject = null;

        public void Exchange(GameObject part)
        {
            if (partObject != null)
                Destroy(partObject);

            partObject = part;

            if (partObject != null)
                partObject.transform.SetParent(this.transform, false);
        }
    }
}
