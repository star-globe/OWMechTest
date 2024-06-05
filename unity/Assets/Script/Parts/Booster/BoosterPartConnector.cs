using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BoosterPartConnector : PartConnector
    {
        [SerializeField]
        BoostVector boostVector;
        public BoostVector BoostVector => boostVector;
    }
}
