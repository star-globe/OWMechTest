using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    public class BaseCharacter : BaseObject
    {
        [SerializeField]
        PlayerParameterSettings paramSettings;

        public CharacterParam CharacterParam { get; private set; } = null;

        public override int Ap    => CharacterParam?.Ap    ?? int.MinValue;
        public override int MaxAp => CharacterParam?.MaxAp ?? int.MinValue;

        public override void Initialize(long id, UnitSide side)
        {
            base.Initialize(id, side);
            CharacterParam = new CharacterParam();
            CharacterParam.SetInitialInfo(paramSettings);
        }

        public bool IsBoost
        {
            get
            {
                return CharacterParam.IsBoost;
            }
            set
            {
                CharacterParam.SetBoost(value);
            }
        }

        public bool IsQuickBoost
        {
            get
            {
                return CharacterParam.IsQuickBoost;
            }
        }

        public void IgnitJumpBoost()
        {
            CharacterParam.SetJumpBoost(true);
            CharacterParam.ConsumeJumpEnergy();
        }

        public void QuitJumpBoost()
        {
            CharacterParam.SetJumpBoost(false);
        }

        public void IgnitQuickBoost()
        {
            CharacterParam.SetQuickBoost(true);
            CharacterParam.ConsumeQuickEnergy();
        }

        public void QuitQuickBoost()
        {
            CharacterParam.SetQuickBoost(false);
        }

        public bool IsHyperBoost
        {
            get
            {
                return CharacterParam.IsHyperBoost;
            }
            set
            {
                CharacterParam.SetHyperBoost(value);
            }
        }

        public void RecoveryEnegy(float time)
        {
            CharacterParam.Recovery(time);
        }

        public bool IsAscend
        {
            get
            {
                return CharacterParam.IsAscend;
            }
            set
            {
                CharacterParam.SetAscend(value);
            }
        }

        public void UpdateParam(float time, bool isFloat)
        {
            // ブースト ON の間の消費は回復力に含まれている（CharacterParam.EnergyRecovery）
            CharacterParam.Recovery(time);

            if (IsAscend)
            {
                CharacterParam.ConsumeAscendEnergy(time);
            }

            if (IsHyperBoost)
            {
                CharacterParam.ConsumeHyperBoostEnergy(time);
            }
        }

        public void SetSpeed(float speed)
        {
            CharacterParam.SetSpeed(speed);
        }
    }
}
