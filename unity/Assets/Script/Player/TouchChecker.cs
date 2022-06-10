using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class TouchChecker : MonoBehaviour
    {
        [SerializeField]
        float groundDeg = 60.0f;

        [SerializeField]
        float wallDeg = 120.0f;

        CapsuleCollider capsule = null;
        CapsuleCollider Capsule
        {
            get
            {
                capsule = capsule ?? GetComponent<CapsuleCollider>();
                return capsule;
            }
        }

        private readonly Dictionary<int, ValueTuple<TouchState,Vector3>> touchStateDic = new Dictionary<int, ValueTuple<TouchState, Vector3>>();

        private ValueTuple<TouchState, Vector3>? CurrentStateTuple
        {
            get
            {
                ValueTuple<TouchState, Vector3>? tuple = null;
                foreach (var kvp in touchStateDic)
                {
                    if (tuple != null && tuple.Value.Item1 == TouchState.Wall)
                        break;

                    var state = kvp.Value.Item1;
                    if (state == TouchState.None)
                        continue;

                    tuple = kvp.Value;
                }

                return tuple;
            }
        }

        public TouchState CurrentTouchState
        {
            get
            {
                var tuple = this.CurrentStateTuple;
                if (tuple == null)
                    return TouchState.None;

                return tuple.Value.Item1;
            }
        }

        public Vector3 CurrentNormal
        {
            get
            {
                var tuple = this.CurrentStateTuple;
                if (tuple == null)
                    return Vector3.zero;

                return tuple.Value.Item2;
            }
        }

        void OnCollisionEnter(Collision collision)
        {
            var colId = collision.gameObject.GetInstanceID();
            foreach (ContactPoint contact in collision.contacts)
            {
                var state = CheckPoint(contact.point, out var normal);
                if (state == TouchState.None)
                    continue;

                if (touchStateDic.TryGetValue(colId, out var tuple) &&
                    tuple.Item1 == TouchState.Ground)
                    continue;

                touchStateDic[colId] = (state,normal);
            }
        }

        void OnCollisionExit(Collision collision)
        {
            var colId = collision.gameObject.GetInstanceID();
            touchStateDic.Remove(colId);
        }

        private TouchState CheckPoint(Vector3 point, out Vector3 normal)
        {
            normal = Vector3.zero;

            if (this.Capsule == null)
                return TouchState.None;

            var vec = Vector3.zero;
            switch (this.Capsule.direction)
            {
                case 0: vec = Vector3.right; break;
                case 1: vec = Vector3.up; break;
                case 2: vec = Vector3.forward; break;
                default:
                    return TouchState.None;
            }

            var underCenter = this.Capsule.center - vec * (this.Capsule.height / 2 - this.Capsule.radius);
            underCenter = this.Capsule.transform.TransformPoint(underCenter);

            var diff = point - underCenter;
            var cos = Vector3.Dot(diff, Vector3.down) / diff.magnitude;
            if (cos > Mathf.Cos(groundDeg * Mathf.Deg2Rad)) {
                normal = Vector3.up;
                return TouchState.Ground;
            }

            if (cos > Mathf.Cos(wallDeg * Mathf.Deg2Rad)) {
                normal = -diff.normalized;
                return TouchState.Wall;
            }

            return TouchState.None;
        }
    }

    public enum TouchState
    {
        None = 0,
        Ground,
        Wall,
    }
}
