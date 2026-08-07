using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic Object Pool để reuse GameObjects
/// </summary>
/// <typeparam name="T">Component type (ví dụ: Segment, Coin)</typeparam>
public class ObjectPool<T> where T : Component
{
    private T prefab;
    private Transform parent;
    private Queue<T> availableObjects = new Queue<T>();
    private List<T> allObjects = new List<T>();
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prefab">Prefab để pool</param>
    /// <param name="initialSize">Số lượng objects ban đầu</param>
    /// <param name="parent">Parent transform (để organize hierarchy)</param>
    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        
        // Pre-instantiate objects
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }
    
    /// <summary>
    /// Tạo object mới và thêm vào pool
    /// </summary>
    private T CreateNewObject()
    {
        T newObj = GameObject.Instantiate(prefab, parent);
        newObj.gameObject.SetActive(false); // Disable ban đầu
        
        availableObjects.Enqueue(newObj);
        allObjects.Add(newObj);
        
        return newObj;
    }
    
    /// <summary>
    /// Lấy object từ pool
    /// </summary>
    public T Get()
    {
        T obj;
        
        if (availableObjects.Count > 0)
        {
            // Lấy từ pool
            obj = availableObjects.Dequeue();
        }
        else
        {
            // Pool hết → tạo mới (expand pool)
            obj = CreateNewObject();
            Debug.LogWarning($"Pool expanded! Consider increasing initial size.");
        }
        
        obj.gameObject.SetActive(true);
        return obj;
    }
    
    /// <summary>
    /// Trả object về pool
    /// </summary>
    public void Return(T obj)
    {
        if (obj == null)
        {
            Debug.LogError("Cannot return null object to pool!");
            return;
        }
        
        obj.gameObject.SetActive(false);
        availableObjects.Enqueue(obj);
    }
    
    /// <summary>
    /// Trả tất cả objects về pool
    /// </summary>
    public void ReturnAll()
    {
        foreach (T obj in allObjects)
        {
            if (obj != null && obj.gameObject.activeSelf)
            {
                Return(obj);
            }
        }
    }
    
    /// <summary>
    /// Get count của available objects
    /// </summary>
    // public int AvailableCount => availableObjects.Count;
    public int AvailableCount {
        get {
            return availableObjects.Count;
        }
    }
    
    /// <summary>
    /// Get count của tất cả objects (active + inactive)
    /// </summary>
    // public int TotalCount => allObjects.Count;
    public int TotalCount {
        get {
            return allObjects.Count;
        }
    }

    /// <summary>
    /// Pre-warm pool
    /// </summary>
    /// <param name="count">Số lượng objects để pre-warm</param>
    /// <returns>Số lượng objects đã pre-warm</returns>
    /// </summary>
    public void PreWarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateNewObject();
        }
    }
}