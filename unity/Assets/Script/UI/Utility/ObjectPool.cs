using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectPool<T> where T : Component
{
    [SerializeField]
    RectTransform content = null;

    [SerializeField]
    T baseObject = null;

    private readonly Queue<T> sleepQueue = new Queue<T>();
    private readonly List<T> activeList = new List<T>();

    public T Borrow()
    {
        if (baseObject == null)
            return null;

        T comp = null;
        if (sleepQueue.Count > 0)
        {
            comp = sleepQueue.Dequeue();
        }
        else
        {
            var go = UnityEngine.Object.Instantiate(baseObject.gameObject, content);
            comp = go.GetComponent<T>();
        }

        if (comp != null)
        {
            comp.gameObject.SetActive(true);
            activeList.Add(comp);
        }

        return comp;
    }

    public void Return(T comp)
    {
        if (comp == null)
            return;

        comp.gameObject.SetActive(false);

        activeList.Remove(comp);
        sleepQueue.Enqueue(comp);
    }

    public void ReturnAll()
    {
        foreach (var comp in activeList)
        {
            comp.gameObject.SetActive(false);
            sleepQueue.Enqueue(comp);
        }

        activeList.Clear();
    }
}
