using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BoosterContainer : MonoBehaviour
    {
        [SerializeField]
        PartConnector[] connectorTrans = null;

        readonly List<BoosterPart> boosters = new List<BoosterPart>();

        public void SetBoosters(int partId)
        {
            boosters.Clear();

            foreach (var connector in connectorTrans)
            {
                var part = BoosterMaster.InstantiateBoosterPart(partId);
                connector?.Exchange(part?.gameObject);

                if (part == null)
                    continue;

                boosters.Add(part);
            }
        }

        public void RemoveBoosters()
        {
            SetBoosters(-1);
        }

        public void Boost(bool isOn)
        {
            foreach (var bst in boosters)
                bst.Boost(isOn);
        }
    }
}
