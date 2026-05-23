using UnityEngine;

namespace AdvancedGears
{
    public class FixedBaseObject : BaseObject
    {
        protected virtual void Awake()
        {
            Initialize(gameObject.GetInstanceID(), UnitSide.None);
        }
    }
}
