using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BoosterPart : PartComponent
    {
        [SerializeField]
        ParticleSystem particle;

        public void Boost(bool isOn)
        {
            if (particle != null)
            {
                if (isOn)
                    particle.Play();
                else
                    particle.Stop();
            }
        }
    }
}
