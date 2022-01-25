using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitLoadFields());
    }

    IEnumerator WaitLoadFields()
    {
        while (FieldManager.Instance.IsLoadingFields)
        {
            yield return null;
        }

        GetSpawnContainer();

        SpawnPlayer();
    }

    private List<SpawnPointContainer> containers = new List<SpawnPointContainer>();

    private void GetSpawnContainer()
    {
        containers.Clear();
        foreach (var scene in FieldManager.Instance.FieldScenes)
        {
            if (scene == null)
                continue;

            var goList = scene.Value.GetRootGameObjects();
            foreach (var go in goList)
            {
                var container = go.GetComponent<SpawnPointContainer>();
                if (container != null)
                    containers.Add(container);
            }
        }
    }

    private void SpawnPlayer()
    {
        Transform trans = null;
        foreach (var c in containers)
        {
            var p = c.GetSpawnPoint(0);
            if (p != null)
            {
                trans = p.transform;
                break;
            }
        }

        if (trans != null)
            PlayerManager.Instance.CreatePlayer(isSelf: true, 0, trans.position, trans.rotation);
        else
            Debug.LogErrorFormat("There is no SpawnPoint. ID:{0}", FieldManager.Instance.CurrentFieldID);
    }
}
