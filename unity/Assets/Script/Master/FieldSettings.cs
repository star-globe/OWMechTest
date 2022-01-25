using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "TestProject/Field/FieldSettings", order = 0)]
public class FieldSettings : ScriptableObject
{
    [SerializeField]
    string fieldName;
    public string FieldName => fieldName;

    [SerializeField]
    int fieldId;
    public int FieldID => fieldId;

    [SerializeField]
    string folder;

    [SerializeField]
    List<string> fieldSceneNames = null;
    public List<string> FieldSceneNames => fieldSceneNames;

#if UNITY_EDITOR
    public void SearchFieldScenes()
    {
        fieldSceneNames.Clear();

        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:scene", new string[] { folder });
        foreach (var g in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
            //var name = System.IO.Path.GetFileName(path);

            Debug.LogFormat("Folder:{0} Add Name:{1}", folder, path);

            fieldSceneNames.Add(path);
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
