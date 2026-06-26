using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemiesDataSO : ScriptableObject
{
    public List<EnemyData> enemiesData;
    public EnemyData GetEnemyDataByID(int id)
    {
        foreach (var data in enemiesData)
        {
            if (data.ID == id)
                return data;
        }

        Debug.LogWarning($"EnemyData with ID {id} not found.");
        return null;
    }
}

[Serializable]
public class EnemyData
{
    [field: SerializeField]
    public string Name;
    [field: SerializeField]
    public int ID;
    [field: SerializeField] 
    public Sprite Icon;
    [field: SerializeField]
    public RuntimeAnimatorController Animator;
    [field: SerializeField]
    public int Range;
}