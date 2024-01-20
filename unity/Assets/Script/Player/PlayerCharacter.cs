using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    public class PlayerCharacter : BaseCharacter
    {
        public bool IsSelf { get; private set; }

        public void Initialize(long id, UnitSide side, bool isSelf)
        {
            base.Initialize(id, side);
            this.IsSelf = isSelf;
        }

        public float PlayerHeight
        {
            get
            {
                return SelfHeight;
            }
        }
    }
}
