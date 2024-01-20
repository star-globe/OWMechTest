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
            PartMaster.Instance.Load();
            GunMaster.Instance.Load();
            BulletMaster.Instance.Load();
            BoosterMaster.Instance.Load();
            EffectMaster.Instance.Load();

            PlayerControllerConst.Instance.Load();
        }
    }

    public interface IDBasedMasterSettings
    {
        int ID { get; }
    }

    public class IDBasedMasterContainer<T> where T : ScriptableObject, IDBasedMasterSettings
    {
        private readonly Dictionary<int, T> SettingsDic = new Dictionary<int, T>();

        public IDBasedMasterContainer(){}

        public T GetSettings(int id)
        {
            if (SettingsDic.TryGetValue(id, out var settings) == false)
            {
                Debug.LogErrorFormat("There is no {0}. Id:{1}", this.GetType(), id);
                return null;
            }

            return settings;
        }

        public void Clear()
        {
            SettingsDic.Clear();
        }

        public void LoadRecord(T record)
        {
            SettingsDic[record.ID] = record;
        }

        public Dictionary<int, T> GetDictionary()
        {
            return SettingsDic;
        }

        protected virtual string resourcesFolder { get; } = null;

        public virtual void Load()
        {
            var settings = Resources.LoadAll<T>(resourcesFolder);

            Clear();
            foreach (var set in settings)
            {
                LoadRecord(set);
            }
        }
    }
}
