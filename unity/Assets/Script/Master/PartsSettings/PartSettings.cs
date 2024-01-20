using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    public enum PartCategoryType
    {
        None = 0,

        Head = 101,
        Core,
        Arm,
        Leg,

        Booster = 201,

        Gun = 301,
    }

    public class PartSettings : ScriptableObject, IDBasedMasterSettings
    {
        [SerializeField]
        int partId;
        public int PartID => partId;

        [SerializeField]
        PartCategoryType categoryType;
        public PartCategoryType CategoryType => categoryType;

        [SerializeField]
        PartComponent partComponent;

        public int ID => PartID;

        public PartComponent InstantiatePart()
        {
            PartComponent inst = null;
            if (partComponent != null)
            {
                inst = Instantiate(partComponent);
                inst.SetInfo(this.ID);
            }
            else
            {
                Debug.LogError("PartComponent is not Attached.");
            }

            return inst;
        }

        public T InstantiatePart<T>() where T : PartComponent
        {
            var part = InstantiatePart();
            if (part == null)
                return null;

            var comp = part as T;
            if (comp == null)
            {
                Debug.LogErrorFormat("{0} is not Attached", typeof(T));
            }

            return comp;
        }
    }

    public class PartMasterContainer : IDBasedMasterContainer<PartSettings>
    {
        protected override string resourcesFolder => "PartSettings";

        public override void Load()
        {
            base.Load();

            LoadInheritDic(GunMaster.Instance.GetDictionary());
            LoadInheritDic(BoosterMaster.Instance.GetDictionary());
        }

        private void LoadInheritDic<T>(Dictionary<int, T> dic) where T : PartSettings
        {
            foreach (var kvp in dic)
                LoadRecord(kvp.Value);
        }
    }

    public class PartMaster : Singleton<PartMasterContainer>
    {
        public static GameObject InstantiatePartPrefab(int partId)
        {
            return Instance.GetSettings(partId)?.InstantiatePart()?.gameObject;
        }
    }
}
