using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T :MonoBehaviour
{
    private static T instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                var comp = SingletonMonoObject.Instance;
                if (comp != null && comp.gameObject != null)
                {
                    instance = comp.gameObject.AddComponent<T>();
                }
            }

            return instance;
        }
    }

    private void Awake()
    {
        //if (Instance == null)
        //    Instance = this as T;

        DontDestroyOnLoad(this.gameObject);
    }
}
