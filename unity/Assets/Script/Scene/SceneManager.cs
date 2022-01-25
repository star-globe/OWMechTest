using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManager : SingletonMonobehaviour<SceneManager>
{
    bool isNowLoading = false;

    public void GameStart()
    {
        LoadSelectMenu();
    }

    public void LoadSelectMenu()
    {
        LoadScene("SelectMenu");
    }

    public void LoadBattleScene()
    {
        LoadScene("Battle");
    }

    public void LoadScene(string sceneName)
    {
        if (isNowLoading)
        {
            Debug.LogError("Now Loading Another Scene.");
        }

        StartCoroutine(LoadSceneAsnc(sceneName));
    }

    IEnumerator LoadSceneAsnc(string sceneName)
    {
        isNowLoading = true;

        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (op.isDone == false)
            yield return null;

        isNowLoading = false;
    }
}
