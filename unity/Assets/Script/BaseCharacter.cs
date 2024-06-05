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

        public CharacterParam CharacterParam { get; private set; } = null;

        public long ID { get; private set; }
        public UnitSide Side { get; private set; }

        public void Initialize(long id, UnitSide side)
        {
            this.ID = id;
            this.Side = side;
            CharacterParam = new CharacterParam();
            CharacterParam.SetInitialInfo(1000, 1000, 400);
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
    }
}
