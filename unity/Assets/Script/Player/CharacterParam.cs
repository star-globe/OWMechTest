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
        public int MaxEnergy => EnergyPerformance.MaxEnergy;

        /// <summary>エネルギーの性能（最大エネルギーと回復力）。装備から計算した値を SetEnergyPerformance で設定する</summary>
        public EnergyPerformance EnergyPerformance { get; private set; }

        /// <summary>今の回復力（1 秒あたり）。ブースト ON の間は下がる</summary>
        public int EnergyRecovery => EnergyPerformance.GetRecovery(IsBoost);

        public float Speed { get; private set; } = 0;

        public int JumpEnergy { get; private set; }
        public int QuickBoostEnergy { get; private set; }
        public int HyperBoostEnergy { get; private set; }
        public int AscendEnergy { get; private set; }

        public bool IsBoost { get; private set; } = false;
        public bool IsJumpBoost { get; private set; } = false;
        public bool IsQuickBoost { get; private set; } = false;
        public bool IsHyperBoost { get; private set; } = false;
        public bool IsAscend { get; private set; } = false;

        public int LockCircleSize { get; private set; }
        public int LockLength { get; private set; }

        public void SetInitialInfo(PlayerParameterSettings param)
        {
            Ap = param.MaxAP;
            MaxAp = param.MaxAP;

            // 装備から性能を計算するまで（またはジェネレーターを読み込めないとき）は、機体の設定の値を使う
            SetEnergyPerformance(EnergyPerformance.FromFixedValues(param.MaxEnergy, param.EnergyRecovery, param.BoostEnergy));

            JumpEnergy = param.JumpEnergy;
            QuickBoostEnergy = param.QuickBoostEnergy;
            HyperBoostEnergy = param.HyperBoostEnergy;
            AscendEnergy = param.AscendEnergy;

            LockCircleSize = param.LockCircleSize;
            LockLength = param.LockLength;
        }

        /// <summary>エネルギーの性能を設定し、エネルギーを満タンにする</summary>
        public void SetEnergyPerformance(EnergyPerformance performance)
        {
            EnergyPerformance = performance;
            Energy = performance.MaxEnergy;
        }

        public void SetDamage(int damage)
        {
            Ap = Mathf.Max(Ap - damage, 0);
        }

        public void ConsumeEnergy(int consume)
        {
            Energy = Mathf.Max(Energy - consume, 0);
        }

        /// <summary>今の回復力で回復する（ブースト ON の間の消費は回復力に含まれている）</summary>
        public void Recovery(float time)
        {
            var add = (int) (EnergyRecovery * time);
            Energy = Mathf.Clamp(Energy + add, 0, MaxEnergy);
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

        public void ConsumeAscendEnergy(float time)
        {
            ConsumeEnergy((int) (time * AscendEnergy));
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

        /// <summary>グライド中の上昇は、エネルギーが残っていれば続けられる</summary>
        public bool CanAscend
        {
            get { return Energy > 0; }
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

        public void SetAscend(bool ascend)
        {
            this.IsAscend = ascend;
        }

        public void SetSpeed(float speed)
        {
            this.Speed = speed;
        }
    }
}
