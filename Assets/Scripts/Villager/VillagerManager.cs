using System;
using System.Collections.Generic;
using UnityEngine;

public class VillagerManager : MonoBehaviour
{
    public event Action<int> OnVillagerBought;
    public event Action<int> OnVillagerHoused;
    public event Action<int, int, bool> OnVillagerRemoved; // villagerIndex, inVillageIndex, isEmployed
    public event Action<int, int, Villager> OnVillagerAssigned; // buildingIndex, villagerSlot, villager
    public event Action<int, int, int, string> OnVillagerEmployed; // inVillageIndex, previousVillagerInVillageIndex, prevVillagerIndex, buildingName
    public event Action<int, int> OnVillagerRemovedWithoutReplacement; // villagerIndex, inVillageIndex
    public event Action<Villager, bool> OnVillagerTotallyRemoved; // villager, removedFromVillage

    [SerializeField] private GameObject villagerPrefab;
    [SerializeField] private PlacementSystem placementManager;
    [SerializeField] private VillagersDataSO villagerDatabase;
    [SerializeField] private VillagerInventoryManager inventoryManager;

    private readonly List<Villager> villagersHeld = new();
    private readonly List<int> villagersHeldID = new();
    private readonly List<Villager> villagersInVillage = new();
    private readonly List<int> villagerInVillageIndex = new(); // maps in-village slot -> villagersHeld index

    public int SelectedBuildingIndex { get; private set; } = -1;
    public int SelectedVillagerSlot { get; private set; } = -1;
    public bool IsHouseSelection { get; private set; } = false;

    public int VillagersHeldCount => villagersHeld.Count;
    public int VillagersInVillageCount => villagersInVillage.Count;

    public void SelectBuildingSlot(int buildingIndex, int villagerSlot, bool buildingIsHouse)
    {
        SelectedBuildingIndex = buildingIndex;
        SelectedVillagerSlot = villagerSlot;
        IsHouseSelection = buildingIsHouse;

        if (IsHouseSelection)
        {
            inventoryManager.OpenAllInventory();
        }
        else
        {
            inventoryManager.OpenAvailableInventory();
        }
    }

    public void selectBuildingSlot(int buildingIndex, int villagerSlot, bool buildingIsHouse)
    {
        SelectBuildingSlot(buildingIndex, villagerSlot, buildingIsHouse);
    }

    public void BuyVillager(int villagerID)
    {
        if (villagersHeldID.Contains(villagerID))
            return;

        VillagerData villagerData = villagerDatabase.GetVillagerDataByID(villagerID);
        if (villagerData == null)
            return;

        villagersHeldID.Add(villagerID);

        GameObject villagerGO = Instantiate(villagerPrefab, transform);
        Villager villager = villagerGO.GetComponent<Villager>();

        villager.Initialize(villagerData);
        villager.Index = villagersHeld.Count;

        villagersHeld.Add(villager);

        OnVillagerBought?.Invoke(villagerID);
    }

    public void buyVillager(int villagerID)
    {
        BuyVillager(villagerID);
    }

    public void AssignVillagerToBuilding(int villagerIndex)
    {
        if (!IsValidHeldVillagerIndex(villagerIndex) || !HasValidSelectedBuilding())
        {
            ResetSelection();
            return;
        }

        int targetBuildingIndex = SelectedBuildingIndex;
        int targetVillagerSlot = SelectedVillagerSlot;
        bool assigningToHouse = IsHouseSelection;

        Villager villager = villagersHeld[villagerIndex];

        if (assigningToHouse)
        {
            HouseVillager(villager, villagerIndex, targetBuildingIndex, targetVillagerSlot);
        }

        Building selectedBuilding = GetPlacedBuilding(targetBuildingIndex);
        if (selectedBuilding == null)
        {
            ResetSelection();
            return;
        }

        Villager previousVillager = selectedBuilding.GetVillagerInSlot(targetVillagerSlot);

        int inVillageIndex = GetInVillageIndex(villagerIndex);
        int previousInVillageIndex = -1;
        int previousVillagerIndex = -1;

        if (previousVillager != null)
        {
            previousVillagerIndex = previousVillager.Index;
            previousInVillageIndex = GetInVillageIndex(previousVillagerIndex);
            HandlePreviousVillagerReplacement(selectedBuilding, previousVillager, previousVillagerIndex, assigningToHouse);
        }

        selectedBuilding.AssignVillagerToSlot(targetVillagerSlot, villager);

        if (assigningToHouse)
        {
            OnVillagerHoused?.Invoke(villagerIndex);
        }
        else
        {
            HandleEmploymentAssignment(
                villager,
                inVillageIndex,
                previousInVillageIndex,
                previousVillagerIndex,
                selectedBuilding.BuildingName,
                targetBuildingIndex,
                targetVillagerSlot);
        }

        OnVillagerAssigned?.Invoke(targetBuildingIndex, targetVillagerSlot, villager);
        ResetSelection();
    }

    public void AssignVillagertoBuilding(int villagerIndex)
    {
        AssignVillagerToBuilding(villagerIndex);
    }

    public void RemoveVillagerFromBuilding(int villagerIndex)
    {
        if (!IsValidHeldVillagerIndex(villagerIndex))
            return;

        Villager villager = villagersHeld[villagerIndex];
        if (!villager.isEmployed || villager.assignedBuildingIndex < 0 || villager.assignedBuildingSlot < 0)
            return;

        Building assignedBuilding = GetPlacedBuilding(villager.assignedBuildingIndex);
        if (assignedBuilding == null)
            return;

        int inVillageIndex = GetInVillageIndex(villagerIndex);

        assignedBuilding.RemoveVillagerFromSlot(villager.assignedBuildingSlot);
        OnVillagerRemovedWithoutReplacement?.Invoke(villagerIndex, inVillageIndex);
        OnVillagerTotallyRemoved?.Invoke(villager, false);

        villager.Unemploy();
    }

    public void RemoveVillagerFromVillage(int villagerIndex, bool villagerIsEmployed)
    {
        if (!IsValidHeldVillagerIndex(villagerIndex))
            return;

        Villager villager = villagersHeld[villagerIndex];
        Building house = GetPlacedBuilding(villager.assignedHouseIndex);
        if (house == null)
            return;

        if (villagerIsEmployed)
        {
            Building building = GetPlacedBuilding(villager.assignedBuildingIndex);
            if (building != null)
            {
                OnVillagerTotallyRemoved?.Invoke(villager, false);
                building.RemoveVillagerFromSlot(villager.assignedBuildingSlot);
            }

            villager.Unemploy();
        }

        int inVillageIndex = GetInVillageIndex(villagerIndex);
        if (inVillageIndex < 0)
            return;

        villagerInVillageIndex.RemoveAt(inVillageIndex);
        villagersInVillage.RemoveAt(inVillageIndex);

        OnVillagerRemoved?.Invoke(villagerIndex, inVillageIndex, villagerIsEmployed);
        OnVillagerTotallyRemoved?.Invoke(villager, true);

        house.RemoveVillagerFromSlot(villager.assignedHouseSlot);
        villager.RemoveFromVillage();
    }

    public void CancelSelection()
    {
        ResetSelection();
    }

    public void CloseAllInventorySelection()
    {
        inventoryManager.CloseAllInventory();
        ResetSelection();
    }

    public void CloseAvailableInventorySelection()
    {
        inventoryManager.CloseAvailableInventory();
        ResetSelection();
    }

    public bool HasVillagerIDAt(int villagerIndex)
    {
        return villagerIndex >= 0 && villagerIndex < villagersHeldID.Count;
    }

    public int GetVillagerIDAt(int villagerIndex)
    {
        if (!HasVillagerIDAt(villagerIndex))
            return -1;

        return villagersHeldID[villagerIndex];
    }

    public bool HasHeldVillagerAt(int villagerIndex)
    {
        return villagerIndex >= 0 && villagerIndex < villagersHeld.Count;
    }

    public Villager GetHeldVillagerAt(int villagerIndex)
    {
        if (!HasHeldVillagerAt(villagerIndex))
            return null;

        return villagersHeld[villagerIndex];
    }

    public bool IsVillagerInVillage(int villagerIndex)
    {
        return GetInVillageIndex(villagerIndex) != -1;
    }

    public int GetInVillageIndexForHeldVillager(int villagerIndex)
    {
        return GetInVillageIndex(villagerIndex);
    }

    private void HouseVillager(Villager villager, int villagerIndex, int buildingIndex, int villagerSlot)
    {
        if (villager.isHoused)
            return;

        villagersInVillage.Add(villager);
        villagerInVillageIndex.Add(villagerIndex);
        villager.Housed(buildingIndex, villagerSlot);
    }

    private void HandlePreviousVillagerReplacement(Building selectedBuilding, Villager previousVillager, int previousVillagerIndex, bool assigningToHouse)
    {
        if (assigningToHouse)
        {
            RemoveVillagerFromVillage(previousVillagerIndex, previousVillager.isEmployed);
            return;
        }

        int previousVillagerSlot = previousVillager.assignedBuildingSlot;
        selectedBuilding.RemoveVillagerFromSlot(previousVillagerSlot);
        previousVillager.Unemploy();
    }

    private void HandleEmploymentAssignment(
        Villager villager,
        int inVillageIndex,
        int previousInVillageIndex,
        int previousVillagerIndex,
        string buildingName,
        int buildingIndex,
        int villagerSlot)
    {
        if (villager.isEmployed)
        {
            Building previousBuilding = GetPlacedBuilding(villager.assignedBuildingIndex);
            if (previousBuilding != null)
            {
                previousBuilding.RemoveVillagerFromSlot(villager.assignedBuildingSlot);
            }

            OnVillagerTotallyRemoved?.Invoke(villager, false);
        }

        OnVillagerEmployed?.Invoke(inVillageIndex, previousInVillageIndex, previousVillagerIndex, buildingName);
        villager.Employed(buildingIndex, villagerSlot);
    }

    private Building GetPlacedBuilding(int buildingIndex)
    {
        if (placementManager == null)
            return null;

        if (buildingIndex < 0 || buildingIndex >= placementManager.placedGameObjects.Count)
            return null;

        GameObject buildingObject = placementManager.placedGameObjects[buildingIndex];
        if (buildingObject == null)
            return null;

        return buildingObject.GetComponent<Building>();
    }

    private int GetInVillageIndex(int villagerIndex)
    {
        return villagerInVillageIndex.IndexOf(villagerIndex);
    }

    private bool IsValidHeldVillagerIndex(int villagerIndex)
    {
        return villagerIndex >= 0 && villagerIndex < villagersHeld.Count;
    }

    private bool HasValidSelectedBuilding()
    {
        return SelectedBuildingIndex >= 0 && SelectedVillagerSlot >= 0;
    }

    private void ResetSelection()
    {
        SelectedBuildingIndex = -1;
        SelectedVillagerSlot = -1;
        IsHouseSelection = false;
    }
}