using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Runtime.CompilerServices;
using UnityEngine.UI;

namespace AdvancedGears
{
    public enum UnitSide
    {
        None = 0,
        Alpha,
        Bravo
    }

    public static class UnitSideExtensions
    {
        // NPC用の除外マスク
        public static int ToNpcExcludeMask(this UnitSide side)
        {
            int mask = 1 << (int) side;
            if (side != UnitSide.None)
                mask |= 1 << (int) UnitSide.None;
            return mask;
        }
    }

    public class BaseObject : MonoBehaviour
    {
        public long ID { get; private set; }
        public UnitSide Side { get; private set; }

        public virtual int Ap    => int.MinValue;
        public virtual int MaxAp => int.MinValue;

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

        #region 速度と照準点（偏差射撃で使う）

        Rigidbody _rigid;
        bool _rigidChecked;
        Collider _aimCollider;
        bool _aimColliderChecked;

        bool hasPrevPosition;
        Vector3 prevPosition;
        Vector3 trackedVelocity;

        /// <summary>
        /// 現在の速度。物理で動く Rigidbody があれば linearVelocity、なければ前フレームとの位置の差分から求める。
        /// 移動を式で計算しているユニットは override して正確な速度を返してよい
        /// </summary>
        public virtual Vector3 Velocity
        {
            get
            {
                if (_rigidChecked == false)
                {
                    TryGetComponent(out _rigid);
                    _rigidChecked = true;
                }

                if (_rigid != null && _rigid.isKinematic == false)
                    return _rigid.linearVelocity;

                return trackedVelocity;
            }
        }

        /// <summary>射撃で狙う点（コライダーの中心。なければ Transform の位置）</summary>
        public virtual Vector3 AimPosition
        {
            get
            {
                if (_aimColliderChecked == false)
                {
                    TryGetComponent(out _aimCollider);
                    _aimColliderChecked = true;
                }

                return _aimCollider != null ? _aimCollider.bounds.center : transform.position;
            }
        }

        /// <summary>
        /// 位置の差分から速度を記録する。派生クラスで LateUpdate を定義するときは override して base.LateUpdate() を呼ぶこと
        /// </summary>
        protected virtual void LateUpdate()
        {
            var pos = transform.position;
            var delta = Time.deltaTime;
            if (hasPrevPosition && delta > 0f)
                trackedVelocity = (pos - prevPosition) / delta;

            prevPosition = pos;
            hasPrevPosition = true;
        }

        #endregion
    }
}
