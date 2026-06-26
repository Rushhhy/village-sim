using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Campaign/Level Config")]
public class CampaignLevelConfig : ScriptableObject
{
    [Header("Level Info")]
    public int campaignIndex;
    public int levelIndex;
    public bool isHard;

    [Header("Enemies")]
    public List<EnemySpawnData> enemies = new();

    [Header("Deployment Zone")]
    public Vector2Int deployMin = new Vector2Int(0, 0);
    public Vector2Int deployMax = new Vector2Int(3, 2);

    [Header("Star Goals")]
    public int starRoundTarget = 5;

    [Header("Map")]
    public GameObject mapPrefab;
}