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

        /// <param name="prioritySources">ブースターパーツの色より優先する色の供給元（内装など）。優先度の高い順</param>
        public void SetBoosters(int partId, IReadOnlyList<IBoosterColorSource> prioritySources = null)
        {
            boosters.Clear();

            var baseColor = ResolveBaseColor(partId, prioritySources);

            foreach (var connector in connectorTrans)
            {
                var part = BoosterMaster.InstantiateBoosterPart(partId);
                connector?.Exchange(part?.gameObject);

                if (part == null)
                    continue;

                part.SetBoostVector(connector.BoostVector);
                part.SetBaseColor(baseColor);
                boosters.Add(part);
            }
        }

        static Color ResolveBaseColor(int partId, IReadOnlyList<IBoosterColorSource> prioritySources)
        {
            var sources = new List<IBoosterColorSource>();
            if (prioritySources != null)
                sources.AddRange(prioritySources);

            if (partId >= 0)
                sources.Add(new BoosterPartColorSource(BoosterMaster.Instance.GetSettings(partId)));

            return BoosterColorResolver.Resolve(sources, BoosterColorPalette.DefaultBaseColor);
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
