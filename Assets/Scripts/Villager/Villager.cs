using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VillagerState
{
    Base,
    Idle,
    Working,
}

public class Villager : MonoBehaviour, IIdleBehaviorTarget, IWorkBehaviorTarget, IPositionValidationTarget
{
    public VillagerData villagerData;
    public int Index;
    public VillagerState currentState;

    private PlacementSystem placementSystem;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private IdleBehavior idleBehavior;
    [SerializeField] private WorkBehavior workBehavior;
    // REMOVED: private PositionValidator positionValidator;

    #region IIdleBehaviorTarget Implementation
    public Transform Transform => transform;
    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;
    public bool ShouldContinueIdling => currentState == VillagerState.Idle;
    #endregion

    #region IWorkBehaviorTarget Implementation
    // Using the same interface properties as IIdleBehaviorTarget since they're identical
    // Transform, Animator, SpriteRenderer are already implemented above
    #endregion

    // REMOVED: #region IPositionValidationTarget Implementation

    // ... all your existing fields ...
    public int happiness;
    public int level = 1;
    public int upgradePoints = 0;
    public float HEALTH = 100f;
    public float attack;
    public float defense;

    public float evasion;
    public float critChance;

    public int mobility;
    public int range;
    public Animator animator;

    public bool isCavalry = false;
    public bool isRanged = false;
    public bool isHoused = false;
    public bool isEmployed = false;

    public int assignedHouseIndex = -1;
    public int assignedHouseSlot = -1;
    public int assignedBuildingIndex = -1;
    public int assignedBuildingSlot = -1;

    public int assignedBuildingID = -1;
    public Vector3 assignedBuildingPosition = Vector3.zero;

    private void Awake()
    {
        idleBehavior = GetComponent<IdleBehavior>();
        // REMOVED: positionValidator = GetComponent<PositionValidator>();
        workBehavior = GetComponent<WorkBehavior>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(VillagerData vd)
    {
        villagerData = vd;
        placementSystem = GameObject.Find("PlacementSystem").GetComponent<PlacementSystem>();

        animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = villagerData.villagerAnimatorController;

        if (villagerData.tier == 2)
        {
            isCavalry = true;
        }
        if (villagerData.range > 1)
        {
            isRanged = true;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is Villager other)
        {
            return this.Index == other.Index;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    // ... all your existing methods ...
    public void LevelUp() { level++; }
    public void UpgradeDefense() { defense = defense * 1.05f; }
    public void UpgradeAttack() { attack = attack * 1.05f; }
    public void UpgradeEvasion() { evasion = evasion * 1.05f; }
    public void UpgradeCritChance() { critChance = critChance * 1.05f; }

    public void Housed(int houseIndex, int selectedSlot)
    {
        if (placementSystem == null ||
            placementSystem.placedGameObjects == null ||
            houseIndex < 0 ||
            houseIndex >= placementSystem.placedGameObjects.Count ||
            placementSystem.placedGameObjects[houseIndex] == null)
        {
            Debug.LogError($"Invalid house index {houseIndex} for villager {gameObject.name}");
            return;
        }

        GameObject houseObject = placementSystem.placedGameObjects[houseIndex];

        assignedHouseIndex = houseIndex;
        assignedHouseSlot = selectedSlot;
        isHoused = true;

        Vector3 housePosition = houseObject.transform.position;

        if (idleBehavior != null)
        {
            idleBehavior.SetIdleStartPosition(housePosition);
        }

        if (!isEmployed)
        {
            UpdateState();

            if (idleBehavior != null)
            {
                idleBehavior.StartIdling();
            }
        }
        else
        {
            UpdateState();
        }
    }

    public void Employed(int buildingIndex, int selectedSlot)
    {
        if (workBehavior == null)
        {
            Debug.LogError($"WorkBehavior component not found on villager {gameObject.name}");
            return;
        }

        if (placementSystem == null ||
            placementSystem.placedGameObjects == null ||
            buildingIndex < 0 ||
            buildingIndex >= placementSystem.placedGameObjects.Count ||
            placementSystem.placedGameObjects[buildingIndex] == null)
        {
            Debug.LogError($"Invalid building index {buildingIndex} for villager {gameObject.name}");
            return;
        }

        GameObject buildingObject = placementSystem.placedGameObjects[buildingIndex];
        Building building = buildingObject.GetComponent<Building>();

        if (building == null)
        {
            Debug.LogError($"Building component not found on assigned building for villager {gameObject.name}");
            return;
        }

        if (idleBehavior != null && idleBehavior.IsIdling)
        {
            idleBehavior.StopIdling();
        }

        assignedBuildingIndex = buildingIndex;
        assignedBuildingSlot = selectedSlot;
        assignedBuildingID = building.ID;
        assignedBuildingPosition = buildingObject.transform.position;

        isEmployed = true;

        List<Vector3> workPositions = GetWorkPositionsFromBuilding(building);

        workBehavior.InitializeWork(
            assignedBuildingID,
            assignedBuildingPosition,
            workPositions
        );

        UpdateState();
    }

    public void Unemploy()
    {
        if (workBehavior != null)
        {
            workBehavior.StopWorking();
            workBehavior.SetBuildingPos(Vector3.zero);
        }

        assignedBuildingIndex = -1;
        assignedBuildingSlot = -1;
        assignedBuildingID = -1;
        assignedBuildingPosition = Vector3.zero;
        isEmployed = false;

        if (isHoused)
        {
            UpdateState();

            if (idleBehavior != null)
            {
                idleBehavior.SetIdleStartPosition(transform.position);
                idleBehavior.StartIdling();
            }
        }
        UpdateState();
    }

    public void RemoveFromVillage()
    {
        if (idleBehavior != null)
        {
            idleBehavior.StopIdling();
        }

        if (workBehavior != null)
        {
            workBehavior.StopWorking();
        }

        assignedBuildingIndex = -1;
        assignedHouseIndex = -1;
        assignedBuildingSlot = -1;
        assignedHouseSlot = -1;
        assignedBuildingID = -1;
        assignedBuildingPosition = Vector3.zero;

        isHoused = false;
        isEmployed = false;

        transform.position = Vector3.zero;

        if (idleBehavior != null)
        {
            idleBehavior.SetIdleStartPosition(transform.position);
        }

        UpdateState();
    }

    private List<Vector3> GetWorkPositionsFromBuilding(Building building)
    {
        if (building == null || building.workPositions == null)
        {
            return new List<Vector3>();
        }

        return building.workPositions;
    }

    public bool IsValidationEnabled
    {
        get
        {
            if (currentState == VillagerState.Idle)
                return true;

            if (!isEmployed || workBehavior == null)
                return false;

            WorkState state = workBehavior.GetCurrentWorkState();

            return state == WorkState.Transporting ||
                   state == WorkState.Logging;
        }
    }
    private void UpdateState()
    {
        if (isEmployed)
        {
            currentState = VillagerState.Working;
        }
        else if (isHoused)
        {
            currentState = VillagerState.Idle;
        }
        else
        {
            currentState = VillagerState.Base;
        }
    }
    public int GetStars()
    {
        int baseStars = villagerData != null ? villagerData.tier : 1;
        int upgradeStars = upgradePoints / 5;

        return Mathf.Clamp(baseStars + upgradeStars, 1, 5);
    }
}