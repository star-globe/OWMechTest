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

    public class BaseObject : MonoBehaviour
    {
        [SerializeField]
        CapsuleCollider capsuleCollider = null;

        public long ID { get; private set; }
        public UnitSide Side { get; private set; }

        public virtual void Initialize(long id, UnitSide side)
        {
            this.ID = id;
            this.Side = side;
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
    }
}
