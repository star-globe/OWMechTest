using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class FootHolder : ObjectBehaviour
    {
        [SerializeField]
        InputXZComponent xzComponent = null;

        [SerializeField]
        FootIK[] foots = null;

        [SerializeField]
        FootRoot footRoot = null;

        [SerializeField]
        float controllerMagnitude = 1.0f;

        readonly Dictionary<int, List<FootIK>> footDic = new Dictionary<int, List<FootIK>>();
        readonly List<int> keys = new List<int>();
        int holdIndex = 0;

        private void Start()
        {
            footDic.Clear();
            keys.Clear();

            if (foots == null)
                return;

            foreach (var f in foots)
            {
                if (footDic.ContainsKey(f.ID) == false)
                    footDic[f.ID] = new List<FootIK>();

                footDic[f.ID].Add(f);
            }

            keys.AddRange(footDic.Keys);
        }

        private void Update()
        {
            InputUpdate();
            IKUpdate();
        }

        private void InputUpdate()
        {
            float inputX = 0;
            float inputZ = 0;

            if (xzComponent != null)
            {
                xzComponent.InputXZ(out inputX, out inputZ);
            }

            var vec = new Vector3(inputX, 0, inputZ);

            footRoot?.SetRotate(inputX, inputZ);

            vec = vec.normalized * controllerMagnitude;

            vec = this.SelfTrans.TransformDirection(vec);

            foreach (var kvp in footDic) {
                foreach (var f in kvp.Value)
                    f.SetControllerVec(vec);
            }
        }

        private void IKUpdate()
        {
            if (footDic.Count == 0)
                return;

            if (holdIndex < 0 || holdIndex >= keys.Count)
                return;

            var holdKey = keys[holdIndex];

            bool grounded = false;
            foreach (var kvp in footDic) {
                foreach (var f in kvp.Value) {
                if (kvp.Key != holdKey)
                    grounded |= f.IsGrounded;
                }
            }

            if (grounded == false)
                return;

            holdIndex++;
            if (holdIndex >= keys.Count)
                holdIndex = 0;

            var key = keys[holdIndex];
            foreach (var kvp in footDic) {
                bool isHold = key == kvp.Key;
                foreach (var f in kvp.Value)
                    f.SetHold(isHold);
            }
        }
    }
}
