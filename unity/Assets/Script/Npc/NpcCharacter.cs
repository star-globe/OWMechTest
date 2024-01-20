using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    public class NpcCharacter : BaseCharacter
    {
        public bool IsCompany {get;private set;}
        public void Initialize(long id, UnitSide side, bool isCompany)
        {
            base.Initialize(id, side);
            this.IsCompany = isCompany;
        }
    }
}
