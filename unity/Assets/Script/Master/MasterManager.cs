using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class MasterManager : SingletonMonoBehaviour<MasterManager>
    {
        public void LoadAll()
        {
            FieldMaster.Instance.Load();
            PlayerMaster.Instance.Load();
        }
    }
}
