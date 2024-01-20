using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PartComponent : MonoBehaviour
    {
        public int PartId { get; private set;}

        public void SetInfo (int partId)
        {
            this.PartId = partId;
        }
    }
}
