using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BoosterContainer : MonoBehaviour
    {
        [SerializeField]
        BoosterPartConnector[] connectorTrans = null;

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

                part.SetBoostVector(connector.BoostVector);
                boosters.Add(part);
            }
        }

        public void RemoveBoosters()
        {
            SetBoosters(-1);
        }

        public void Boost(int vectorBit)
        {
            foreach (var bst in boosters)
                bst.Boost(vectorBit);
        }

        public void Quick(int vectorBit)
        {
            foreach (var bst in boosters)
                bst.Quick(vectorBit);
        }

        public void HyperBoost(int vectorBit)
        {
            foreach (var bst in boosters)
                bst.HyperBoost(vectorBit);
        }
    }
}
