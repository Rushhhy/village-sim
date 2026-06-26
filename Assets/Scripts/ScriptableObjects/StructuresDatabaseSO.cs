using System;
using System.Collections.Generic;
using UnityEngine;

public enum StructureCategory
{
    Unknown = 0,
    Road = 1,
    Building = 2,
    Decoration = 3,
    RotatableDecoration = 4
}

[CreateAssetMenu]
public class StructuresDatabaseSO : ScriptableObject
{
    [SerializeField] public List<StructureData> objectsData = new();

    private Dictionary<int, StructureData> dataById;
    private bool isCacheBuilt;

    private void OnEnable()
    {
        BuildCache();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        isCacheBuilt = false;
    }
#endif

    public Vector2Int GetSizeByID(int id)
    {
        StructureData data = GetDataByID(id);
        return data != null ? data.Size : Vector2Int.zero;
    }

    public StructureData GetDataByID(int id)
    {
        EnsureCacheBuilt();

        if (dataById.TryGetValue(id, out StructureData data))
        {
            return data;
        }

        Debug.LogWarning($"StructuresDatabaseSO: No structure found with ID {id}.");
        return null;
    }

    public bool TryGetDataByID(int id, out StructureData data)
    {
        EnsureCacheBuilt();
        return dataById.TryGetValue(id, out data);
    }

    private void EnsureCacheBuilt()
    {
        if (!isCacheBuilt)
        {
            BuildCache();
        }
    }

    private void BuildCache()
    {
        dataById = new Dictionary<int, StructureData>();

        if (objectsData == null)
        {
            objectsData = new List<StructureData>();
            isCacheBuilt = true;
            return;
        }

        for (int i = 0; i < objectsData.Count; i++)
        {
            StructureData data = objectsData[i];

            if (data == null)
            {
                Debug.LogWarning($"StructuresDatabaseSO: Null entry found at index {i}.");
                continue;
            }

            if (dataById.ContainsKey(data.ID))
            {
                Debug.LogWarning($"StructuresDatabaseSO: Duplicate structure ID {data.ID} found. Keeping the first entry.");
                continue;
            }

            dataById.Add(data.ID, data);
        }

        isCacheBuilt = true;
    }
}

[Serializable]
public class StructureData
{
    [field: SerializeField]
    public string Name { get; private set; }

    [field: SerializeField]
    public int ID { get; private set; }

    [field: SerializeField]
    public StructureCategory Category { get; private set; } = StructureCategory.Unknown;

    [field: SerializeField]
    public GameObject Base { get; private set; }

    [field: SerializeField]
    public GameObject LevelOnePrefab { get; private set; }

    [field: SerializeField]
    public GameObject LevelTwoPrefab { get; private set; }

    [field: SerializeField]
    public GameObject LevelThreePrefab { get; private set; }

    [field: SerializeField]
    public Vector2Int Size { get; private set; } = Vector2Int.one;
}