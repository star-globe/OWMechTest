using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class CharacterParam
    {
        public int Ap { get; private set; }
        public int MaxAp { get; private set; }

        /// <summary>
        /// エネルギーの残量。毎フレームの回復・消費は 1 未満になることがあるため、内部では小数で持ち、
        /// 切り捨てた値を返す（int で毎フレーム切り捨てると、フレームレートが高いほど回復・消費が少なくなる）
        /// </summary>
        public int Energy => (int) energy;
        float energy;

        public int MaxEnergy => EnergyPerformance.MaxEnergy;

        /// <summary>
        /// オーバーヒート中か。消費して残量がマイナスに達すると（ちょうど 0 では入らない）、残量を 0 にしてオーバーヒートする。
        /// オーバーヒート中はエネルギーを使う行動ができず、通常の回復をしない代わりに、残量が 0 から復帰エネルギーまで
        /// オーバーヒート時間をかけて一定の速さで増え、復帰エネルギーに達したところで解除される
        /// </summary>
        public bool IsOverheat { get; private set; } = false;

        /// <summary>オーバーヒートしてからの経過時間（秒）。オーバーヒート中でなければ 0</summary>
        public float OverheatElapsed { get; private set; } = 0f;

        /// <summary>オーバーヒート時間（秒）。装備しているジェネレーターで決まる</summary>
        public float OverheatTime => EnergyPerformance.OverheatTime;

        /// <summary>復帰エネルギー（オーバーヒートの解除時の残量）。装備しているジェネレーターで決まる</summary>
        public int OverheatRecoveryEnergy => EnergyPerformance.OverheatRecoveryEnergy;

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
            SetEnergyPerformance(EnergyPerformance.FromFixedValues(param.MaxEnergy, param.EnergyRecovery, param.BoostEnergy,
                                                                   param.OverheatTime, param.OverheatRecoveryRate));

            JumpEnergy = param.JumpEnergy;
            QuickBoostEnergy = param.QuickBoostEnergy;
            HyperBoostEnergy = param.HyperBoostEnergy;
            AscendEnergy = param.AscendEnergy;

            LockCircleSize = param.LockCircleSize;
            LockLength = param.LockLength;
        }

        /// <summary>エネルギーの性能を設定し、エネルギーを満タンにする（オーバーヒートも解除する）</summary>
        public void SetEnergyPerformance(EnergyPerformance performance)
        {
            EnergyPerformance = performance;
            energy = performance.MaxEnergy;
            IsOverheat = false;
            OverheatElapsed = 0f;
        }

        public void SetDamage(int damage)
        {
            Ap = Mathf.Max(Ap - damage, 0);
        }

        public void ConsumeEnergy(int consume)
        {
            ConsumeEnergy((float) consume);
        }

        /// <summary>消費する。残量がマイナスに達したら 0 にしてオーバーヒートする。オーバーヒート中は消費しない</summary>
        void ConsumeEnergy(float consume)
        {
            if (IsOverheat || consume <= 0f)
                return;

            var next = energy - consume;
            if (next < 0f)
            {
                energy = 0f;
                IsOverheat = true;
                OverheatElapsed = 0f;
                return;
            }

            energy = next;
        }

        /// <summary>
        /// 今の回復力で回復する（ブースト ON の間の消費は回復力に含まれている）。
        /// オーバーヒート中は、残量を 0 から復帰エネルギーまで時間に比例して増やし、オーバーヒート時間が経ったら解除する
        /// </summary>
        public void Recovery(float time)
        {
            if (IsOverheat)
            {
                UpdateOverheat(time);
                return;
            }

            energy = Mathf.Clamp(energy + EnergyRecovery * time, 0f, MaxEnergy);
        }

        void UpdateOverheat(float time)
        {
            OverheatElapsed += time;

            var recoveryEnergy = Mathf.Min(OverheatRecoveryEnergy, MaxEnergy);
            if (OverheatElapsed >= OverheatTime)
            {
                IsOverheat = false;
                OverheatElapsed = 0f;
                energy = recoveryEnergy;
                return;
            }

            energy = recoveryEnergy * (OverheatElapsed / OverheatTime);
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
            ConsumeEnergy(time * HyperBoostEnergy);
        }

        public void ConsumeAscendEnergy(float time)
        {
            ConsumeEnergy(time * AscendEnergy);
        }

        /// <summary>エネルギーを使う行動は、オーバーヒートしていなければ（残量が 0 以上なら）実行できる</summary>
        public bool CanUseEnergy => !IsOverheat;

        public bool CanJump => CanUseEnergy;

        public bool CanQuick => CanUseEnergy;

        public bool CanHyperBoost => CanUseEnergy;

        /// <summary>グライド中の上昇は、オーバーヒートするまで続けられる</summary>
        public bool CanAscend => CanUseEnergy;

        /// <summary>
        /// 消費量より多く残っているか（オーバーヒート中は false）。
        /// NPC の行動の判断に使う（Can* だけで判断すると、行動を連発してオーバーヒートを繰り返すため）
        /// </summary>
        public bool HasEnergyFor(int cost)
        {
            return CanUseEnergy && Energy > cost;
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
