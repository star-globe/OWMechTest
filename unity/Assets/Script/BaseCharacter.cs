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

        public void UpdateParam(float time, bool isFloat)
        {
            CharacterParam.Recovery(time);

            if (IsBoost && isFloat)
            {
                CharacterParam.ConsumeBoostEnergy(time);
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
