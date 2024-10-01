using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    public enum UnitSide
    {
        None = 0,
        Alpha,
        Bravo
    }

    public class BaseCharacter : MonoBehaviour
    {
        [SerializeField]
        CapsuleCollider capsuleCollider = null;

        [SerializeField]
        PlayerParameterSettings paramSettings;

        public CharacterParam CharacterParam { get; private set; } = null;

        public long ID { get; private set; }
        public UnitSide Side { get; private set; }

        public void Initialize(long id, UnitSide side)
        {
            this.ID = id;
            this.Side = side;
            CharacterParam = new CharacterParam();
            CharacterParam.SetInitialInfo(paramSettings);
        }

        public float SelfHeight
        {
            get
            {
                if (capsuleCollider != null)
                    return capsuleCollider.height;

                return 0;
            }
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

        public void IgnitQuickBoost()
        {
            CharacterParam.SetQuickBoost(true);
            CharacterParam.ConsumeQuick();
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

            if (IsBoost && !isFloat)
            {
                CharacterParam.ConsumeBoostEnergy(time);
            }

            if (IsHyperBoost)
            {
                CharacterParam.ConsumeHyper(time);
            }
        }
    }
}
