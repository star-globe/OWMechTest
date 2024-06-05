using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public enum BoostVector : byte
    {
        Forward = 0,
        Left = 1,
        Right = 2,
        Back = 3,
    }

    public class BoosterPart : PartComponent
    {
        [SerializeField]
        ParticleSystem particle;

        [SerializeField]
        ParticleSystem quick;

        int boostBit = 0;

        public void SetBoostVector(BoostVector vec)
        {
            boostBit = 1 >> (byte) vec;
        }

        public void Boost(int vectorBit)
        {
            if (particle != null)
            {
                bool isOn = (boostBit & vectorBit) != 0;
                if (isOn)
                    particle.Play();
                else
                    particle.Stop();
            }
        }

        public void Quick(int vectorBit)
        {
            if (quick != null && (boostBit & vectorBit) != 0)
            {
                quick.Play();
            }
        }
    }
}
