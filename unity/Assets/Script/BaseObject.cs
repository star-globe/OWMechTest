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
        public long ID { get; private set; }
        public UnitSide Side { get; private set; }

        public virtual void Initialize(long id, UnitSide side)
        {
            this.ID = id;
            this.Side = side;
        }

        float? _selfHeight = null;
        public float SelfHeight
        {
            get
            {
                if (_selfHeight.HasValue)
                    return _selfHeight.Value;

                var col = GetComponent<Collider>();
                if (col is CapsuleCollider capsule)
                    _selfHeight = capsule.height;
                else if (col is BoxCollider box)
                    _selfHeight = box.size.y;
                else if (col is SphereCollider sphere)
                    _selfHeight = sphere.radius * 2f;
                else
                    _selfHeight = 0f;

                return _selfHeight.Value;
            }
        }
    }
}
