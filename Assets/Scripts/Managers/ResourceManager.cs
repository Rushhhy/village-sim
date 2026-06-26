using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public int[] resourceTotals = new int[13];
    public float[] resourceProductionTotals = new float[13];
    public float[] resourceConsumptionTotals = new float[13];

    [Header("Currency")]
    public int coins = 0;
    public int gems = 0;

    public event Action<int> OnCoinsChanged;
    public event Action<int> OnGemsChanged;

    [Header("Storage")]
    public int baseCapacity = 100;
    public int maxCapacity = 100;

    private string[] resourceNames = new string[]
    {
        "Wheat", "Wood", "Tools", "Stone", "Iron", "Hardwood",
        "Leather", "Meat", "Flour", "Bread", "Clothing",
        "Furniture", "Gems"
    };

    private void Awake()
    {
        maxCapacity = baseCapacity;
    }

    private void Start()
    {
        OnCoinsChanged?.Invoke(coins);
        OnGemsChanged?.Invoke(gems);
    }

    public void AddCoins(int amount)
    {
        int gain = Mathf.Max(0, amount);

        if (gain <= 0)
        {
            return;
        }

        coins += gain;
        OnCoinsChanged?.Invoke(coins);
    }

    public void AddGems(int amount)
    {
        int gain = Mathf.Max(0, amount);

        if (gain <= 0)
        {
            return;
        }

        gems += gain;
        OnGemsChanged?.Invoke(gems);
    }

    public int GetUsedCapacity()
    {
        int totalUsed = 0;

        for (int i = 0; i < resourceTotals.Length; i++)
        {
            totalUsed += resourceTotals[i];
        }

        return totalUsed;
    }

    public int GetMaxCapacity()
    {
        return maxCapacity;
    }

    public int GetFreeCapacity()
    {
        return Mathf.Max(0, maxCapacity - GetUsedCapacity());
    }

    public bool CanStoreAmount(int amount)
    {
        return GetUsedCapacity() + amount <= maxCapacity;
    }

    public bool CanStoreResource(int resourceID, int amount)
    {
        if (resourceID < 0 || resourceID >= resourceTotals.Length)
        {
            return false;
        }

        if (amount < 0)
        {
            return false;
        }

        return CanStoreAmount(amount);
    }

    public bool TryAddResource(int resourceID, int amount)
    {
        if (resourceID < 0 || resourceID >= resourceTotals.Length)
        {
            return false;
        }

        if (amount < 0)
        {
            return false;
        }

        if (!CanStoreAmount(amount))
        {
            return false;
        }

        resourceTotals[resourceID] += amount;
        return true;
    }

    public bool HasEnoughResource(int resourceID, int amount)
    {
        if (resourceID < 0 || resourceID >= resourceTotals.Length)
        {
            return false;
        }

        if (amount < 0)
        {
            return false;
        }

        return resourceTotals[resourceID] >= amount;
    }

    public bool TryConsumeResource(int resourceID, int amount)
    {
        if (!HasEnoughResource(resourceID, amount))
        {
            return false;
        }

        resourceTotals[resourceID] -= amount;
        return true;
    }

    public void PrintTotals()
    {
        string totals = "Resource Totals:\n";

        for (int i = 0; i < Mathf.Min(resourceTotals.Length, resourceNames.Length); i++)
        {
            totals += $"{resourceNames[i]}: {resourceTotals[i]}\n";
        }

        totals += $"\nCapacity: {GetUsedCapacity()}/{GetMaxCapacity()}";

        Debug.Log(totals);
    }
}