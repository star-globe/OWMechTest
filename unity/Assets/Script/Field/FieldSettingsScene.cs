using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

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

        var scenes = new List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
        var count = scenes.Count;

        foreach (var settings in settingsList)
        {
            foreach (var name in settings.FieldSceneNames)
            {
                var index = scenes.FindIndex(scene => string.Equals(scene.path, name));
                if (index < 0)
                {
                    scenes.Add(new UnityEditor.EditorBuildSettingsScene(name, true));
                }
            }
        }

        if (count != scenes.Count)
        {
            UnityEditor.EditorBuildSettings.scenes = scenes.ToArray();
        }
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
