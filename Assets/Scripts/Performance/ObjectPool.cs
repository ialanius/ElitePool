using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Object Pool - Reuses GameObjects instead of Instantiate/Destroy
/// Great for particles, effects, sounds
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        public bool expandable = true;
    }

    [Header("Pools")]
    public List<Pool> pools;

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    [Header("Debug")]
    public bool showDebugLogs = false;

    void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePools();
    }

    void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            // Create initial pool
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);

            if (showDebugLogs)
                Debug.Log("[ObjectPool] Created pool: " + pool.tag + " (Size: " + pool.size + ")");
        }
    }

    /// <summary>
    /// Spawn object from pool
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("[ObjectPool] Pool with tag " + tag + " doesn't exist!");
            return null;
        }

        GameObject objectToSpawn;

        // Get from pool or create new if expandable
        if (poolDictionary[tag].Count > 0)
        {
            objectToSpawn = poolDictionary[tag].Dequeue();
        }
        else
        {
            // Pool is empty
            Pool pool = pools.Find(p => p.tag == tag);

            if (pool != null && pool.expandable)
            {
                // Create new object
                objectToSpawn = Instantiate(pool.prefab);
                objectToSpawn.transform.SetParent(transform);

                if (showDebugLogs)
                    Debug.Log("[ObjectPool] Expanded pool: " + tag);
            }
            else
            {
                Debug.LogWarning("[ObjectPool] Pool " + tag + " is empty and not expandable!");
                return null;
            }
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    /// <summary>
    /// Return object to pool
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("[ObjectPool] Pool with tag " + tag + " doesn't exist!");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        poolDictionary[tag].Enqueue(obj);
    }

    /// <summary>
    /// Spawn and auto-return after delay
    /// </summary>
    public GameObject SpawnFromPoolTimed(string tag, Vector3 position, Quaternion rotation, float lifetime)
    {
        GameObject obj = SpawnFromPool(tag, position, rotation);

        if (obj != null)
        {
            StartCoroutine(ReturnAfterDelay(tag, obj, lifetime));
        }

        return obj;
    }

    System.Collections.IEnumerator ReturnAfterDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null)
        {
            ReturnToPool(tag, obj);
        }
    }
}

// ================================================================
// POOLABLE OBJECT - Add this to objects in the pool
// ================================================================

public class PoolableObject : MonoBehaviour
{
    public string poolTag;
    public float autoReturnTime = -1f; // -1 = don't auto return

    void OnEnable()
    {
        if (autoReturnTime > 0f)
        {
            Invoke(nameof(ReturnToPool), autoReturnTime);
        }
    }

    void OnDisable()
    {
        CancelInvoke();
    }

    public void ReturnToPool()
    {
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.ReturnToPool(poolTag, gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}