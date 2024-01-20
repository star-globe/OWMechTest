using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class FootRoot : ObjectBehaviour
    {
        [SerializeField]
        float lerpTime = 1.0f;

        float lerp = 1.0f;
        Vector3 target = Vector3.forward;

        const float diffMin = 0.1f;

        public void SetRotate(float x, float z)
        {
            Vector3 tgt = Vector3.forward;
            if (x != 0 || z != 0) {
                tgt = new Vector3(x, 0, z);
                tgt.Normalize();

                if (Vector3.Dot(tgt, Vector3.forward) < 0)
                    tgt = -tgt;
            }

            var diff = tgt - target;

            if (lerp >= 1.0f || diff.sqrMagnitude > diffMin * diffMin)
                lerp = 0.0f;

            target = tgt;
        }

        private void SlerpRotate()
        {
            if (lerp >= 1.0f)
                return;

            var foward = this.SelfTrans.forward;
            this.SelfTrans.forward = Vector3.Lerp(foward, target, lerp);

            lerp += Time.deltaTime / lerpTime;
        }

        private void Update()
        {
            SlerpRotate();
        }
    }
}
