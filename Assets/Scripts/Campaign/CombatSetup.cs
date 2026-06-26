using UnityEngine;

public class CombatSetup : MonoBehaviour
{
    [SerializeField] private GridManager grid;
    [SerializeField] private CampaignLevelDatabase levelDatabase;
    [SerializeField] private DeploymentManager deploymentManager;
    [SerializeField] private BattleManager battleManager;

    private void Start()
    {
        CampaignLevelConfig config = levelDatabase.GetLevel(
            CampaignBattleData.campaignIndex,
            CampaignBattleData.levelIndex,
            CampaignBattleData.isHard
        );

        if (config == null)
            return;

        SpawnMap(config);
        grid.RebuildGrid();
        ApplyDeploymentZone(config);
        deploymentManager.RebuildDeploymentZone();

        CombatCameraController.Instance?.CenterOnTileArea(
            grid,
            config.deployMin,
            config.deployMax,
            5f
        );
        SpawnEnemies(config);
        ApplyBattleSettings(config);
    }

    private void ApplyDeploymentZone(CampaignLevelConfig config)
    {
        if (deploymentManager != null)
        {
            deploymentManager.SetDeploymentZone(config.deployMin, config.deployMax);
        }
    }

    private void SpawnEnemies(CampaignLevelConfig config)
    {
        foreach (EnemySpawnData spawn in config.enemies)
        {
            if (spawn.enemyPrefab == null)
                continue;

            Tile tile = grid.GetTile(spawn.spawnCoord);

            if (tile == null)
                continue;

            Unit enemy = Instantiate(spawn.enemyPrefab);
            enemy.Place(tile);
        }
    }

    private void ApplyBattleSettings(CampaignLevelConfig config)
    {
        if (battleManager != null)
        {
            battleManager.SetStarRoundTarget(config.starRoundTarget);
        }
    }
    private void SpawnMap(CampaignLevelConfig config)
    {
        if (config.mapPrefab != null)
        {
            Instantiate(config.mapPrefab, grid.transform.position, Quaternion.identity);
        }
    }
}