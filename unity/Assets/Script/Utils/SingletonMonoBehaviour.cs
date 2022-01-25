using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonMonobehaviour<T> : MonoBehaviour where T :MonoBehaviour
{
    public static T Instance { get; private set;}

    private void Awake()
    {
        if (Instance == null)
            Instance = this as T;

        DontDestroyOnLoad(this.gameObject);
    }
}
