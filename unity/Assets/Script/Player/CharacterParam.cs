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

        public float Speed { get; private set; } = 0;

        public int BoostEnergy { get; private set; }
        public int JumpEnergy { get; private set; }
        public int QuickBoostEnergy { get; private set; }
        public int HyperBoostEnergy { get; private set; }

        public bool IsBoost { get; private set; } = false;
        public bool IsJumpBoost { get; private set; } = false;
        public bool IsQuickBoost { get; private set; } = false;
        public bool IsHyperBoost { get; private set; } = false;

        public int LockCircleSize { get; private set; }
        public int LockLength { get; private set; }

        public void SetInitialInfo(PlayerParameterSettings param)
        {
            Ap = param.MaxAP;
            MaxAp = param.MaxAP;

            Energy = param.MaxEnergy;
            MaxEnergy = param.MaxEnergy;
            EnergyRecovery = param.EnergyRecovery;


            BoostEnergy = param.BoostEnergy;

            JumpEnergy = param.JumpEnergy;
            QuickBoostEnergy = param.QuickBoostEnergy;
            HyperBoostEnergy = param.HyperBoostEnergy;

            LockCircleSize = param.LockCircleSize;
            LockLength = param.LockLength;
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

        public void ConsumeBoostEnergy(float time)
        {
            ConsumeEnergy((int)(time * BoostEnergy));
        }

        public void ConsumeJumpEnergy()
        {
            ConsumeEnergy(JumpEnergy);
        }

        public void ConsumeQuickEnergy()
        {
            ConsumeEnergy(QuickBoostEnergy);
        }

        public void ConsumeHyperBoostEnergy(float time)
        {
            ConsumeEnergy((int) (time * HyperBoostEnergy));
        }

        public bool CanBoost
        {
            get { return Energy > BoostEnergy; }
        }

        public bool CanJump
        {
            get { return Energy > JumpEnergy; }
        }

        public bool CanQuick
        {
            get { return Energy > QuickBoostEnergy; }
        }

        public bool CanHyperBoost
        {
            get { return Energy > HyperBoostEnergy; }
        }

        public void SetBoost(bool boost)
        {
            this.IsBoost = boost;
        }

        public void SetJumpBoost(bool jump)
        {
            this.IsJumpBoost = jump;
        }

        public void SetQuickBoost(bool quick)
        {
            this.IsQuickBoost = quick;
        }

        public void SetHyperBoost(bool hyper)
        {
            this.IsHyperBoost = hyper;
        }

        public void SetSpeed(float speed)
        {
            this.Speed = speed;
        }
    }
}
