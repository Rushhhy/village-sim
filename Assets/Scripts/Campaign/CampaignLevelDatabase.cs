using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Campaign/Level Database")]
public class CampaignLevelDatabase : ScriptableObject
{
    public List<CampaignLevelConfig> levels = new();

    public CampaignLevelConfig GetLevel(int campaignIndex, int levelIndex, bool isHard)
    {
        foreach (CampaignLevelConfig level in levels)
        {
            if (level.campaignIndex == campaignIndex &&
                level.levelIndex == levelIndex &&
                level.isHard == isHard)
            {
                return level;
            }
        }

        Debug.LogWarning($"No level config found for campaign {campaignIndex}, level {levelIndex}, hard={isHard}");
        return null;
    }
}