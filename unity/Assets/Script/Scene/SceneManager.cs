using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace AdvancedGears
{
    public class SceneManager : SingletonMonoBehaviour<SceneManager>
    {
        public void Register()
        {
            StateManager.Instance.RegisterStateEvent(GameState.Select, LoadSelectMenu, this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Battle, LoadBattleScene, this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Result, LoadResultScene, this.gameObject);
        }

        bool isNowLoading = false;

        private void LoadSelectMenu(Unit unit)
        {
            LoadScene("SelectMenu");
        }

        private void LoadBattleScene(Unit unit)
        {
            LoadScene("Battle");
        }

        private void LoadResultScene(Unit unit)
        {
            LoadScene("Result");
        }

        private void LoadScene(string sceneName)
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
}
