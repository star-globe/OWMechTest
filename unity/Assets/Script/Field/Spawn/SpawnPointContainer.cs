using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class SpawnPointContainer : MonoBehaviour
    {
        [SerializeField]
        List<SpawnPoint> points = null;
        public List<SpawnPoint> Points => points;

        private Dictionary<int, SpawnPoint> pointDictionary = null;
        public Dictionary<int, SpawnPoint> PointDictionary
        {
            get
            {
                if (pointDictionary == null)
                {
                    pointDictionary = new Dictionary<int, SpawnPoint>();

                    foreach (var p in points)
                        pointDictionary[p.ID] = p;
                }

                return pointDictionary;
            }
        }

        private void Awake()
        {
            FieldManager.Instance.SetSpawnPoints(this.PointDictionary);
        }

        private void OnDestroy()
        {
            FieldManager.Instance?.RemoveSpawnPoints(this.PointDictionary.Keys);
        }

        public SpawnPoint GetSpawnPoint(int id)
        {
            this.PointDictionary.TryGetValue(id, out var point);
            return point;
        }

        public void CorrectPoints()
        {
            if (points == null)
                points = new List<SpawnPoint>();

            points.Clear();
            foreach (var p in FindObjectsOfType<SpawnPoint>())
                points.Add(p);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SpawnPointContainer))]
    public class SpawnPointContainerEditor : UnityEditor.Editor
    {
        SpawnPointContainer container = null;

        private void OnEnable()
        {
            container = target as SpawnPointContainer;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("CorrectPoints"))
                container?.CorrectPoints();
        }
    }
#endif
}
