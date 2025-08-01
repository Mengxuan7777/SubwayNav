using System.Collections.Generic;
using UnityEngine;

public class RiderPooler : MonoBehaviour
{
    [System.Serializable]
    public class PoolItem
    {
        public GameObject prefab;
        public int size = 60;
    }

    public List<PoolItem> prefabList;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary = new Dictionary<GameObject, Queue<GameObject>>();

    void Awake()
    {
        foreach (PoolItem item in prefabList)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < item.size; i++)
            {
                GameObject obj = Instantiate(item.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(item.prefab, objectPool);
        }
    }

    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(prefab))
        {
            Debug.LogWarning($"No pool for prefab: {prefab.name}");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[prefab];

        if (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
            return obj;
        }
        else
        {
            Debug.LogWarning($"Pool exhausted for prefab: {prefab.name}");
            return null;
        }
    }

    public void ReturnObject(GameObject prefab, GameObject obj)
    {
        obj.SetActive(false);
        if (!poolDictionary.ContainsKey(prefab))
        {
            poolDictionary[prefab] = new Queue<GameObject>();
        }
        poolDictionary[prefab].Enqueue(obj);
    }

    public GameObject GetRandomPrefab()
    {
        if (prefabList.Count == 0) return null;
        int index = Random.Range(0, prefabList.Count);
        return prefabList[index].prefab;
    }
}
