using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointContainer : MonoBehaviour
{
    [SerializeField]
    List<SpawnPoint> points = null;
    public List<SpawnPoint> Points => points;

    private Dictionary<int,SpawnPoint> pointDictionary = null;
    public Dictionary<int,SpawnPoint> PointDictionary
    {
        get
        {
            if (pointDictionary == null) {
                pointDictionary = new Dictionary<int,SpawnPoint>();

                foreach (var p in points)
                    pointDictionary[p.ID] = p;
            }

            return pointDictionary;
        }
    }

    public SpawnPoint GetSpawnPoint(int id)
    {
        this.PointDictionary.TryGetValue(id, out var point);
        return point;
    }

    public void CorrectPoints()
    {
        points.Clear();
        foreach (var p in FindObjectsOfType<SpawnPoint>())
            points.Add(p);
    }
}
