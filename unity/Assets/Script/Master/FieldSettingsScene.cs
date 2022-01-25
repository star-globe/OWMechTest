using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldSettingsScene : MonoBehaviour
{
    [SerializeField]
    string resourcesFolder = null;

    [SerializeField]
    List<FieldSettings> settingsList = null;


    public void GatherFieldSettings()
    {
        var res = Resources.LoadAll<FieldSettings>(resourcesFolder);
        settingsList = new List<FieldSettings>();
        settingsList.AddRange(res);
    }

#if UNITY_EDITOR
    public void SearchFieldScenes()
    {
        foreach (var settings in settingsList)
            settings.SearchFieldScenes();
    }
#endif
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(FieldSettingsScene))]
public class FieldSettingsSceneEditor : UnityEditor.Editor
{
    FieldSettingsScene scene = null;

    private void OnEnable()
    {
        scene = target as FieldSettingsScene;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("GatherFieldSettings"))
            scene?.GatherFieldSettings();

        if (GUILayout.Button("SearchFieldScenes"))
            scene?.SearchFieldScenes();
    }
}
#endif
