using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class FieldSelectMenu : MonoBehaviour
    {
        [SerializeField]
        string resourcesFolder;

        [SerializeField]
        ObjectPool<FieldSelectButton> pool;

        //readonly Dictionary<int, FieldSettings> settingsDic = new Dictionary<int, FieldSettings>();

        void Start()
        {
            //LoadAllFields();

            ShowFields();
        }

        //private void LoadAllFields()
        //{
        //    settingsDic.Clear();
        //    var settings = Resources.LoadAll<FieldSettings>(resourcesFolder);
        //    foreach (var set in settings)
        //        settingsDic[set.FieldID] = set;
        //}

        private void ShowFields()
        {
            pool.ReturnAll();

            var settingsDic = FieldMaster.Instance.SettingsDic;
            foreach (var kvp in settingsDic)
            {
                var button = pool.Borrow();
                button.SetData(kvp.Key, kvp.Value.FieldName);
                button.SetOnClick(SelectField);
            }
        }

        private void SelectField(int id)
        {
            if (FieldMaster.Instance.SettingsDic.TryGetValue(id, out var fieldSettings))
            {
                StateManager.Instance.NextState();

                foreach (var field in fieldSettings.FieldSceneNames)
                    FieldManager.Instance.AddFieldScene(id, field);
            }
        }
    }
}
