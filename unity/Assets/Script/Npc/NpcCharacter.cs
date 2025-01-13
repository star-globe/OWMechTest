using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace AdvancedGears
{
    public class NpcCharacter : BaseCharacter
    {
        public int LogicID {get;private set;}
        public void Initialize(long id, UnitSide side, int logicId)
        {
            base.Initialize(id, side);
            this.LogicID = logicId;
        }
    }
}
