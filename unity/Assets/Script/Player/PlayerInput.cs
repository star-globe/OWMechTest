using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    CharacterController controller = null;

    [SerializeField]
    float vecSpeed = 1.0f;

    [SerializeField]
    float jumpSpeed = 10.0f;

    const string vertical = "Vertical";
    const string horizontal = "Horizontal";

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        var z = Input.GetAxis(vertical);
        var x = Input.GetAxis(horizontal);
        var mX = InputUtils.GetCenterMouse().x;

        var vec = new Vector3(x, 0, z);
        vec.Normalize();
        vec = controller.transform.TransformDirection(vec);
        vec *= vecSpeed;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            vec += Vector3.up * jumpSpeed;
        }

        controller.SimpleMove(vec);
        controller.transform.Rotate(Vector3.up * mX);
    }
}
