using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedGears
{
    public class SimpleRigidController : ObjectBehaviour
    {
        [SerializeField]
        Rigidbody rigid;

        [SerializeField]
        float speed;

        [SerializeField]
        bool isSlide;

        private void Update()
        {
            UpdateInput();
        }

        private void UpdateInput()
        {
            if (rigid == null)
                return;

            var velocity = rigid.linearVelocity;

            var mag = velocity.x * velocity.x + velocity.z * velocity.z;
            if (mag >= speed * speed)
                return;

            InputUtils.UpdateXZ(out var inputX, out var inputZ);

            Vector3 vec = Vector3.zero;

            if (inputX == 0 && inputZ == 0)
            {
                vec = new Vector3(-velocity.x, 0, -velocity.z);
            }
            else
            {
                vec = new Vector3(inputX, 0, inputZ);
                vec = vec.normalized * speed;

                vec = this.SelfTrans.TransformDirection(vec);
            }

            if (isSlide)
                vec *= Time.deltaTime;

            rigid.AddForce(vec, ForceMode.VelocityChange);
        }
    }
}
