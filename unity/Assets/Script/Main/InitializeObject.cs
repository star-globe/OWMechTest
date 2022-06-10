using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class InitializeObject : MonoBehaviour
    {
        void Start()
        {
            MasterManager.Instance.LoadAll();
            SceneManager.Instance.Register();
            FieldManager.Instance.Register();

            StateManager.Instance.SetStart();
        }
    }
}
