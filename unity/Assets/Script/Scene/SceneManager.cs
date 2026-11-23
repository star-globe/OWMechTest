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
            StateManager.Instance.RegisterStateEvent(GameState.Select,    LoadSelectMenu,       this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Briefing,  LoadBriefingScene,    this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Battle,    LoadBattleScene,      this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Result,    LoadResultScene,      this.gameObject);
            StateManager.Instance.RegisterStateEvent(GameState.Customize, LoadCustomizeScene,   this.gameObject);
        }

        bool isNowLoading = false;

        private void LoadSelectMenu(Unit unit)
        {
            LoadScene("SelectMenu");
        }

        private void LoadBriefingScene(Unit unit)
        {
            LoadScene("Briefing");
        }

        private void LoadBattleScene(Unit unit)
        {
            StartCoroutine(LoadBattleWithFields());
        }

        IEnumerator LoadBattleWithFields()
        {
            isNowLoading = true;
            InputUtils.RecenterOnFocus = true;
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Battle", LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError("Failed to Load Scene. Scene:Battle");
                isNowLoading = false;
                yield break;
            }
            while (!op.isDone)
                yield return null;
            isNowLoading = false;

            // カメラはポインタの中心からのずれで回るため、移行時に中心へ戻す（Briefing のボタン位置に残っているとカメラが動く）
            InputUtils.WarpMouseToCenter();

            // Battle ロード完了後にフィールドシーンを Additive で読み込む
            var fieldManager = FieldManager.Instance;
            if (fieldManager == null)
                yield break;

            fieldManager.LoadPendingMission();

            // フィールドの読み込み中にマウスが動いた場合に備え、読み込み完了後にもう一度中心へ戻す
            while (fieldManager.IsLoadingField)
                yield return null;

            InputUtils.WarpMouseToCenter();
        }

        private void LoadResultScene(Unit unit)
        {
            LoadScene("Result");
        }

        private void LoadCustomizeScene(Unit unit)
        {
            LoadScene("Customize");
        }

        private void LoadScene(string sceneName)
        {
            if (isNowLoading)
            {
                Debug.LogError("Now Loading Another Scene.");
            }

            // 戦闘以外の画面ではフォーカスが戻ってもポインタを動かさない
            InputUtils.RecenterOnFocus = false;

            StartCoroutine(LoadSceneAsnc(sceneName));
        }

        IEnumerator LoadSceneAsnc(string sceneName)
        {
            isNowLoading = true;

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.Log($"Failed to Load Scene. Scene:{sceneName}");
                yield break;
            }

            while (op.isDone == false)
                yield return null;

            isNowLoading = false;
        }
    }
}
