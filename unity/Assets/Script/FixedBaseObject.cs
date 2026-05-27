using UnityEngine;

namespace AdvancedGears
{
    public class FixedBaseObject : BaseObject
    {
        protected virtual void Awake()
        {
            Initialize(gameObject.GetEntityId().GetHashCode(), UnitSide.None);
        }
    }
}
