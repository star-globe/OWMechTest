using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public long ID { get; private set; }
    public bool IsSelf { get; private set; }

    public void Initialize(long id, bool isSelf)
    {
        this.ID = id;
        this.IsSelf = isSelf;
    }
}
