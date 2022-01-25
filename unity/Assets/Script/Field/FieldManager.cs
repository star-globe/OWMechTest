using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FieldManager : SingletonMonobehaviour<FieldManager>
{
    public int CurrentFieldID { get; private set; } = -1;

    readonly Dictionary<string, Scene?> fieldSceneDic = new Dictionary<string, Scene?>();

    public IEnumerable<Scene?> FieldScenes
    {
        get
        {
            foreach (var kvp in fieldSceneDic)
                yield return kvp.Value;
        }
    }

    public bool IsLoadingFields
    {
        get
        {
            if (fieldSceneDic.Count == 0)
                return false;

            foreach (var kvp in fieldSceneDic)
                if (kvp.Value == null)
                    return true;

            return false;
        }
    }

    public void AddFieldScene(int fieldId, string sceneName)
    {
        this.CurrentFieldID = fieldId;

        if (fieldSceneDic.ContainsKey(sceneName))
        {
            Debug.LogErrorFormat("Multiple Field SceneName:{0}", sceneName);
            return;
        }

        fieldSceneDic.Add(sceneName, null);
        StartCoroutine(LoadField(sceneName));
    }

    IEnumerator LoadField(string sceneName)
    {
        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
            yield break;

        while (op.isDone == false)
            yield return null;

        fieldSceneDic[sceneName] = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
    }

    public void ClearFields()
    {
        StartCoroutine(UnloadFields());
    }

    IEnumerator UnloadFields()
    {
        foreach (var kvp in fieldSceneDic)
        {
            if (kvp.Value == null)
                continue;

            var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(kvp.Value.Value);
            while (op.isDone == false)
                yield return null;
        }

        fieldSceneDic.Clear();
    }
}
