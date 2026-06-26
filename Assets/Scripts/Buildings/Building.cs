using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum BuildingState
{
    UnderConstruction,
    Active
}

public class Building : MonoBehaviour
{
    public BuildingState State { get; private set; }

    public float[] constructionDurationsPerLevel = { 5f, 8f, 11f };
    private DateTime constructionEndTime;
    private float constructionDuration;

    public int Index = -1;
    public int ID;
    public int Level = 0;
    public int width;
    public string BuildingName;

    protected BuildingRegistryManager buildingRegistryManager;

    [Header("Sprites")]
    public Sprite BuildingLevelOne;
    public Sprite BuildingLevelTwo;
    public Sprite BuildingLevelThree;

    public Sprite constructionSprite;
    public SpriteRenderer SpriteRenderer;

    [Header("Villagers")]
    public Villager villagerOne;
    public Villager villagerTwo;
    public Villager villagerThree;

    [Header("UI")]
    [SerializeField] private GameObject progressBarPrefab;
    [SerializeField] private GameObject finishConstructionPrefab;
    [SerializeField] private GameObject smokeExplosionPrefab;
    [SerializeField] private GameObject hammerPrefab;

    protected GameObject hammerObj;
    protected GameObject finishConstructionObj;
    protected ConstructionProgressBar currentProgressBar;

    protected Animator animator;
    private bool isAnimated;

    private Transform worldCanvas;
    private VillagerManager villagerManager;

    public List<Vector3> workPositions;
    public List<Vector3> workPositionsOne;
    public List<Vector3> workPositionsTwo;
    public List<Vector3> workPositionsThree;

    [SerializeField] private GameObject resourceGainPopupPrefab;

    protected virtual bool IsHouse => false;

    private Coroutine clickEffectCoroutine;

    protected virtual void Awake()
    {
        buildingRegistryManager = GameObject.Find("BuildingRegistryManager").GetComponent<BuildingRegistryManager>();
        villagerManager = GameObject.Find("VillagerManager").GetComponent<VillagerManager>();

        animator = GetComponent<Animator>();
        isAnimated = animator != null;

        worldCanvas = GameObject.Find("WorldCanvasUI").transform;

        State = BuildingState.Active;
        SetBuildingSprite();
    }

    protected virtual void Update()
    {
        if (State != BuildingState.UnderConstruction)
        {
            return;
        }

        UpdateConstructionUI();

        if (DateTime.UtcNow >= constructionEndTime)
        {
            ShowConstructionFinishedState();
        }
    }

    protected virtual void StartBuildOrUpgrade(int level)
    {
        EnsureConstructionUIExists();

        State = BuildingState.UnderConstruction;

        if (buildingRegistryManager != null)
        {
            buildingRegistryManager.HideBuildingAccessDuringConstruction(Index);
        }

        SmokeEffect();
        SpawnHammer();

        constructionDuration = GetDurationForLevel(level - 1);
        constructionEndTime = DateTime.UtcNow.AddSeconds(constructionDuration);

        if (isAnimated && animator != null)
        {
            animator.enabled = false;
        }

        if (SpriteRenderer != null && constructionSprite != null)
        {
            SpriteRenderer.sprite = constructionSprite;
        }

        currentProgressBar.gameObject.SetActive(true);
        currentProgressBar.SetProgress(0f);
    }

    public virtual void UpgradeBuilding()
    {
        if (Level != 0)
        {
            ClearVillagers();
        }

        StartBuildOrUpgrade(Level);
        Level++;
    }

    public virtual void FinishConstruction()
    {
        State = BuildingState.Active;

        UpdateWorkPositions();

        SmokeEffect();
        SetBuildingSprite();

        if (currentProgressBar != null)
        {
            currentProgressBar.gameObject.SetActive(false);
            Destroy(currentProgressBar.gameObject);
            currentProgressBar = null;
        }

        if (animator != null)
        {
            animator.enabled = true;
        }

        if (hammerObj != null)
        {
            Destroy(hammerObj);
            hammerObj = null;
        }

        if (finishConstructionObj != null)
        {
            finishConstructionObj.SetActive(false);
            Destroy(finishConstructionObj);
            finishConstructionObj = null;
        }

        if (buildingRegistryManager != null)
        {
            buildingRegistryManager.ShowBuildingAccessAfterConstruction(Index);
        }
    }

    public virtual void ClearVillagers()
    {
        if (IsHouse)
        {
            RemoveVillagerFromVillage(villagerOne);
            RemoveVillagerFromVillage(villagerTwo);
            RemoveVillagerFromVillage(villagerThree);
        }
        else
        {
            RemoveVillagerFromBuilding(villagerOne);
            RemoveVillagerFromBuilding(villagerTwo);
            RemoveVillagerFromBuilding(villagerThree);
        }
    }

    public virtual void SetBuildingSprite()
    {
        if (!isAnimated && SpriteRenderer != null)
        {
            SpriteRenderer.sprite = GetSpriteForCurrentLevel();
            return;
        }

        if (animator == null)
        {
            return;
        }

        string[] animationNames =
        {
            "BuildingBaseAnimation",
            "BuildingLevelOneAnimation",
            "BuildingLevelTwoAnimation",
            "BuildingLevelThreeAnimation"
        };

        int layer = 0;
        string fallback = "BuildingLevelOneAnimation";
        string targetAnimation = Level == 0
            ? animationNames[0]
            : animationNames[Mathf.Clamp(Level, 1, 3)];

        if (animator.HasState(layer, Animator.StringToHash(targetAnimation)))
        {
            animator.Play(targetAnimation, layer, 1f);
        }
        else if (animator.HasState(layer, Animator.StringToHash(fallback)))
        {
            animator.Play(fallback, layer, 1f);
        }
    }

    public virtual void AssignVillagerToSlot(int slot, Villager villager)
    {
        switch (slot)
        {
            case 1:
                villagerOne = villager;
                break;
            case 2:
                villagerTwo = villager;
                break;
            case 3:
                villagerThree = villager;
                break;
        }
    }

    public virtual void RemoveVillagerFromSlot(int slot)
    {
        switch (slot)
        {
            case 1:
                villagerOne = null;
                break;
            case 2:
                villagerTwo = null;
                break;
            case 3:
                villagerThree = null;
                break;
        }
    }

    public Villager GetVillagerInSlot(int slot)
    {
        return slot switch
        {
            1 => villagerOne,
            2 => villagerTwo,
            3 => villagerThree,
            _ => null
        };
    }

    protected void SetBuildingNameByLevel(string baseName)
    {
        BuildingName = Level switch
        {
            1 => $"{baseName} LVL1",
            2 => $"{baseName} LVL2",
            3 => $"{baseName} LVL3",
            _ => baseName
        };
    }

    private void EnsureConstructionUIExists()
    {
        if (currentProgressBar == null)
        {
            Vector3 worldPos = GetProgressBarOffset();
            GameObject barGO = Instantiate(progressBarPrefab, worldCanvas);
            currentProgressBar = barGO.GetComponent<ConstructionProgressBar>();
            barGO.transform.position = worldPos;
        }

        if (finishConstructionObj == null)
        {
            Vector3 buttonPos = GetFinishButtonOffset();
            finishConstructionObj = Instantiate(finishConstructionPrefab, worldCanvas);
            finishConstructionObj.transform.position = buttonPos;

            Button finishConstructionButton = finishConstructionObj.GetComponent<Button>();
            finishConstructionButton.onClick.AddListener(FinishConstruction);
        }
    }

    private void UpdateConstructionUI()
    {
        if (currentProgressBar == null)
        {
            return;
        }

        float remainingSeconds = (float)(constructionEndTime - DateTime.UtcNow).TotalSeconds;
        remainingSeconds = Mathf.Max(remainingSeconds, 0f);

        float progress = 1f - Mathf.Clamp01(remainingSeconds / constructionDuration);
        currentProgressBar.SetProgress(progress);

        TimeSpan timeSpan = TimeSpan.FromSeconds(remainingSeconds);
        currentProgressBar.timerText.text = timeSpan.ToString(@"hh\:mm\:ss");
    }

    private void ShowConstructionFinishedState()
    {
        if (finishConstructionObj != null)
        {
            finishConstructionObj.SetActive(true);
        }

        if (currentProgressBar != null)
        {
            currentProgressBar.gameObject.SetActive(false);
        }

        if (hammerObj != null)
        {
            Destroy(hammerObj);
            hammerObj = null;
        }
    }

    private void SpawnHammer()
    {
        if (hammerObj != null)
        {
            Destroy(hammerObj);
        }

        hammerObj = Instantiate(hammerPrefab, GetHammerOffset(), Quaternion.identity);
    }

    private void RemoveVillagerFromVillage(Villager villager)
    {
        if (villager == null)
        {
            return;
        }

        villagerManager.RemoveVillagerFromVillage(villager.Index, villager.isEmployed);
    }

    private void RemoveVillagerFromBuilding(Villager villager)
    {
        if (villager == null)
        {
            return;
        }

        villagerManager.RemoveVillagerFromBuilding(villager.Index);
    }

    public void SmokeEffect()
    {
        SmokeEffectAt(transform.position);
    }

    public void SmokeEffectAt(Vector3 basePosition)
    {
        Vector3 spawnPosition = GetSmokeEffectOffset(basePosition);
        Destroy(Instantiate(smokeExplosionPrefab, spawnPosition, Quaternion.identity), 0.58f);
    }

    private Vector3 GetSmokeEffectOffset(Vector3 basePosition)
    {
        return width switch
        {
            3 => basePosition + new Vector3(-0.1f, -0.5f, 0f),
            4 => basePosition + new Vector3(-1.2f, 0.25f, 0f),
            _ => basePosition + new Vector3(-0.5f, -0.6f, 0f)
        };
    }

    private Sprite GetSpriteForCurrentLevel()
    {
        return Level switch
        {
            0 => BuildingLevelOne,
            1 => BuildingLevelOne,
            2 => BuildingLevelTwo,
            3 => BuildingLevelThree,
            _ => BuildingLevelThree
        };
    }

    private float GetDurationForLevel(int level)
    {
        return constructionDurationsPerLevel[Mathf.Clamp(level, 0, constructionDurationsPerLevel.Length - 1)];
    }

    private Vector3 GetHammerOffset()
    {
        return width switch
        {
            3 => transform.position + new Vector3(1.4f, 1f, 0f),
            4 => transform.position + new Vector3(0f, 1f, 0f),
            _ => transform.position + new Vector3(1f, 1f, 0f)
        };
    }

    private Vector3 GetProgressBarOffset()
    {
        return width switch
        {
            3 => transform.position + new Vector3(0.5f, -0.5f, 0f),
            4 => transform.position + new Vector3(-0.55f, 0.5f, 0f),
            _ => transform.position + new Vector3(0f, -0.5f, 0f)
        };
    }

    private Vector3 GetFinishButtonOffset()
    {
        return width switch
        {
            3 => transform.position + new Vector3(1.5f, -0.4f, 0f),
            4 => transform.position + new Vector3(0.4f, 0.6f, 0f),
            _ => transform.position + new Vector3(1f, -0.4f, 0f)
        };
    }

    private Vector3 GetSmokeEffectOffset()
    {
        return width switch
        {
            3 => transform.position + new Vector3(-0.1f, -0.5f, 0f),
            4 => transform.position + new Vector3(-1.2f, 0.25f, 0f),
            _ => transform.position + new Vector3(-0.5f, -0.6f, 0f)
        };
    }
    public void ShowProducedResourcePopup(Sprite icon, Vector3 worldPosition)
    {
        if (resourceGainPopupPrefab == null)
        {
            Debug.LogWarning("[POPUP] resourceGainPopupPrefab is null.");
            return;
        }

        if (icon == null)
        {
            Debug.LogWarning("[POPUP] icon is null.");
            return;
        }

        if (worldCanvas == null)
        {
            Debug.LogWarning("[POPUP] worldCanvas is null.");
            return;
        }

        GameObject popup = Instantiate(resourceGainPopupPrefab, worldCanvas, false);
        popup.transform.position = worldPosition;
        popup.transform.localScale = Vector3.one;

        Debug.Log($"[POPUP] Popup instantiated under {popup.transform.parent.name}");

        ResourceGainPopup popupScript = popup.GetComponent<ResourceGainPopup>();
        if (popupScript == null)
        {
            Debug.LogWarning("[POPUP] ResourceGainPopup component missing on popup prefab.");
            return;
        }

        popupScript.Initialize(icon);
    }

    protected void UpdateWorkPositions()
    {
        List<Vector3> selectedPositions = null;

        switch (Level)
        {
            case 1:
                selectedPositions = workPositionsOne;
                break;
            case 2:
                selectedPositions = workPositionsTwo;
                break;
            case 3:
                selectedPositions = workPositionsThree;
                break;
        }

        if (selectedPositions != null && selectedPositions.Count > 0)
        {
            workPositions = selectedPositions;
        }
    }

    public void RefreshVillagersAfterMove()
    {
        RefreshVillagerAfterMove(villagerOne);
        RefreshVillagerAfterMove(villagerTwo);
        RefreshVillagerAfterMove(villagerThree);
    }

    private void RefreshVillagerAfterMove(Villager villager)
    {
        if (villager == null)
            return;

        villager.Employed(Index, villager.assignedBuildingSlot);
    }
    public void HideAssignedVillagers()
    {
        SetVillagerVisible(villagerOne, false);
        SetVillagerVisible(villagerTwo, false);
        SetVillagerVisible(villagerThree, false);
    }

    public void ShowAssignedVillagers()
    {
        SetVillagerVisible(villagerOne, true);
        SetVillagerVisible(villagerTwo, true);
        SetVillagerVisible(villagerThree, true);
    }

    private void SetVillagerVisible(Villager villager, bool visible)
    {
        if (villager == null)
            return;

        villager.gameObject.SetActive(visible);
    }
}