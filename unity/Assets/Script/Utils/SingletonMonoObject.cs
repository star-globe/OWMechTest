using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class SingletonMonoObject : MonoBehaviour
    {
        public static SingletonMonoObject Instance { get; private set;}

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            DontDestroyOnLoad(this.gameObject);
        }
    }
}
