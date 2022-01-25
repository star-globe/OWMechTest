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
}
