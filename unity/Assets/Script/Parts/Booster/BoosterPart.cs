using System;
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
        [Serializable]
        class BoosterParticleSystem
        {
            [SerializeField]
            ParticleSystem particle;

            [SerializeField]
            bool isPulse = false;

            bool isOn = false;
            int boostBit = 0;

            public void SetBoostVector(BoostVector vec)
            {
                boostBit = 1 << (byte) vec;
            }

            public void Boost(int vectorBit)
            {
                if (isPulse)
                {
                    particle.Play();
                }
                else
                {
                    bool on = (boostBit & vectorBit) != 0;
                    if (this.isOn == on)
                        return;

                    this.isOn = on;
                    if (isOn)
                        particle.Play();
                    else
                        particle.Stop();
                }
            }
        }

        [SerializeField]
        BoosterParticleSystem normal;

        [SerializeField]
        BoosterParticleSystem quick;

        [SerializeField]
        BoosterParticleSystem hyper;

        int boostBit = 0;
        bool isOn = false;

        public void SetBoostVector(BoostVector vec)
        {
            normal.SetBoostVector(vec);
            quick.SetBoostVector(vec);
            hyper.SetBoostVector(vec);
        }

        public void Boost(int vectorBit)
        {
            normal.Boost(vectorBit);
        }

        public void HyperBoost(int vectorBit)
        {
            hyper.Boost(vectorBit);
        }

        public void Quick(int vectorBit)
        {
            quick.Boost(vectorBit);
        }
    }
}
