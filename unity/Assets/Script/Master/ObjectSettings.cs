using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "TestProject/Object/ObjectSettings", order = 0)]
    public class ObjectSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField]
        int objectId;
        public int ObjectId => objectId;

        [SerializeField]
        GameObject prefab;
        public GameObject Prefab => prefab;

        public int ID => objectId;
    }

    public class ObjectMasterContainer : IDBasedMasterContainer<ObjectSettings>
    {
        protected override string resourcesFolder => "ObjectSettings";
    }

    public class ObjectMaster : Singleton<ObjectMasterContainer>
    {
        public static GameObject GetObjectPrefab(int id)
        {
            return Instance.GetSettings(id)?.Prefab;
        }
    }
}
