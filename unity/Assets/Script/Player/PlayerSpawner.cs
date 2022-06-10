using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AdvancedGears
{
    public class PlayerSpawner : MonoBehaviour
    {
        [SerializeField]
        float inter = 0.1f;

        WaitForSeconds waiter = null;
        WaitForSeconds Waiter
        {
            get
            {
                waiter = new WaitForSeconds(inter);
                return waiter;
            }
        }

        void Start()
        {
            StartCoroutine(WaitLoadFields());
        }

        IEnumerator WaitLoadFields()
        {
            SpawnPoint point = null;
            while (point == null)
            {
                point = FieldManager.Instance.GetSpawnPoint(0);
                yield return this.Waiter;
            }

            SpawnPlayer(point);
        }

        private void SpawnPlayer(SpawnPoint point)
        {
            if (point != null)
            {
                var pos = point.GetGroundedPos();
                var rot = point.transform.rotation;
                PlayerManager.Instance.CreatePlayer(isSelf: true, 0, pos, rot);
            }
            else
                Debug.LogErrorFormat("There is no SpawnPoint. ID:{0}", FieldManager.Instance.CurrentFieldID);
        }
    }
}
