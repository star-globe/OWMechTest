using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class CharacterParam
    {
        public int Ap { get; private set; }
        public int MaxAp { get; private set; }

        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }
        public int EnergyRecovery { get; private set; }

        public bool IsBoost { get; private set; } = false;

        public void SetInitialInfo(int maxAp, int maxEnergy, int energyRecovery)
        {
            Ap = maxAp;
            MaxAp = maxAp;

            Energy = maxEnergy;
            MaxEnergy = maxEnergy;
            EnergyRecovery = energyRecovery;
        }

        public void SetDamage(int damage)
        {
            Ap = Mathf.Max(Ap - damage, 0);
        }

        public void ConsumeEnergy(int consume)
        {
            Energy = Mathf.Max(Energy - consume, 0);
        }

        public void Recovery(float time)
        {
            var add = (int) (EnergyRecovery * time);
            Energy = Mathf.Min(MaxEnergy, Energy + add);
        }

        public void SetBoost(bool boost)
        {
            this.IsBoost = boost;
        }
    }
}
