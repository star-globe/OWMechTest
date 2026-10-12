using UnityEngine;

namespace AdvancedGears
{
    public class ObjectBehaviour : MonoBehaviour
    {
        Transform selfTrans = null;
        protected Transform SelfTrans
        {
            get
            {
                selfTrans = selfTrans ?? this.transform;
                return selfTrans;
            }
        }
    }
}
