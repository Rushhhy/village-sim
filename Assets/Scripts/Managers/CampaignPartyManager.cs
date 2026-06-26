using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CampaignPartyData
{
    public int[] villagerIndexes = { -1, -1, -1, -1, -1, -1 };
}

public class CampaignPartyManager : MonoBehaviour
{
    public CampaignPartyData[] parties =
    {
        new CampaignPartyData(),
        new CampaignPartyData(),
        new CampaignPartyData()
    };

    public int selectedPartyIndex = 0;
    public int mainPartyIndex = 0;

    public bool IsPartyFull(int partyIndex)
    {
        if (partyIndex < 0 || partyIndex >= parties.Length)
            return false;

        for (int i = 0; i < parties[partyIndex].villagerIndexes.Length; i++)
        {
            if (parties[partyIndex].villagerIndexes[i] == -1)
                return false;
        }

        return true;
    }
    public bool SetVillagerInSlot(int partyIndex, int slotIndex, int villagerIndex)
    {
        if (partyIndex < 0 || partyIndex >= parties.Length)
            return false;

        if (slotIndex < 0 || slotIndex >= parties[partyIndex].villagerIndexes.Length)
            return false;

        for (int i = 0; i < parties[partyIndex].villagerIndexes.Length; i++)
        {
            if (i != slotIndex && parties[partyIndex].villagerIndexes[i] == villagerIndex)
                return false;
        }

        parties[partyIndex].villagerIndexes[slotIndex] = villagerIndex;
        return true;
    }
    public bool IsVillagerAlreadyInParty(int partyIndex, int villagerIndex)
    {
        if (partyIndex < 0 || partyIndex >= parties.Length)
            return false;

        for (int i = 0; i < parties[partyIndex].villagerIndexes.Length; i++)
        {
            if (parties[partyIndex].villagerIndexes[i] == villagerIndex)
                return true;
        }

        return false;
    }

    public HashSet<int> GetVillagersInParty(int partyIndex)
    {
        HashSet<int> villagers = new HashSet<int>();

        if (partyIndex < 0 || partyIndex >= parties.Length)
            return villagers;

        for (int i = 0; i < parties[partyIndex].villagerIndexes.Length; i++)
        {
            int villagerIndex = parties[partyIndex].villagerIndexes[i];

            if (villagerIndex != -1)
                villagers.Add(villagerIndex);
        }

        return villagers;
    }
    public void ClearVillagerInSlot(int partyIndex, int slotIndex)
    {
        if (partyIndex < 0 || partyIndex >= parties.Length)
            return;

        if (slotIndex < 0 || slotIndex >= parties[partyIndex].villagerIndexes.Length)
            return;

        parties[partyIndex].villagerIndexes[slotIndex] = -1;
    }
}

