using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace AdvancedGears
{
    public class FieldManager : SingletonMonoBehaviour<FieldManager>
    {
        public int CurrentFieldID { get; private set; } = -1;

        readonly HashSet<string> fieldSceneNames = new HashSet<string>();
        readonly Dictionary<int, SpawnPoint> spawnPoints = new Dictionary<int, SpawnPoint>();

        public void Register()
        {
            StateManager.Instance.RegisterStateEvent(GameState.Result, ClearFields, this.gameObject);
        }

        public void AddFieldScene(int fieldId, string sceneName)
        {
            this.CurrentFieldID = fieldId;

            if (fieldSceneNames.Contains(sceneName))
            {
                Debug.LogErrorFormat("Multiple Field SceneName:{0}", sceneName);
                return;
            }

            fieldSceneNames.Add(sceneName);
            StartCoroutine(LoadField(sceneName));
        }

        IEnumerator LoadField(string sceneName)
        {
            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
                yield break;

            while (op.isDone == false)
                yield return null;
        }

        public void ClearFields()
        {
            ClearFields(Unit.Default);
        }

        private void ClearFields(Unit unit)
        {
            fieldSceneNames.Clear();
        }

        public void UnloadFields()
        {
            StartCoroutine(UnloadFieldsOnScene());
        }

        IEnumerator UnloadFieldsOnScene()
        {
            foreach (var name in fieldSceneNames)
            {
                var op = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(name);
                while (op.isDone == false)
                    yield return null;
            }

            fieldSceneNames.Clear();
        }

        public void SetSpawnPoints(Dictionary<int, SpawnPoint> dic)
        {
            if (dic == null)
                return;

            foreach (var kvp in dic)
                spawnPoints.Add(kvp.Key, kvp.Value);
        }

        public void RemoveSpawnPoints(ICollection<int> keys)
        {
            foreach (var k in keys)
                spawnPoints.Remove(k);
        }

        public SpawnPoint GetSpawnPoint(int id)
        {
            spawnPoints.TryGetValue(id, out var point);
            return point;
        }
    }
}

