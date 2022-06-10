using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class PlayerCharacter : MonoBehaviour
    {
        [SerializeField]
        CapsuleCollider capsuleCollider = null;

        public long ID { get; private set; }
        public bool IsSelf { get; private set; }

        public void Initialize(long id, bool isSelf)
        {
            this.ID = id;
            this.IsSelf = isSelf;
        }

        public float PlayerHeight
        {
            get
            {
                if (capsuleCollider != null)
                    return capsuleCollider.height;

                return 0;
            }
        }
    }
}
