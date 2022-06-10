using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField]
    int id = 0;
    public int ID => id;

    [SerializeField]
    int level;
    public int Level => level;

    public Vector3 GetGroundedPos()
    {
        if (Physics.Raycast(new Ray(this.transform.position + Vector3.up * 1.0f, Vector3.down), out var hit))
            return hit.point;

        return this.transform.position;
    }
}
