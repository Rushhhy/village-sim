using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeploymentManager : MonoBehaviour
{
    [Header("Combat UI")]
    [SerializeField] private GameObject endTurnButton;

    [Header("Deployment UI")]
    [SerializeField] private GameObject deploymentPanel;
    [SerializeField] private Button startButton;

    [Header("Unit Slots UI (frames/buttons)")]
    [Tooltip("Each slot should have a DeploymentUnitSlot component with its Unit assigned.")]
    [SerializeField] private List<DeploymentUnitSlot> unitSlots = new();

    [Header("Refs")]
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;
    [SerializeField] private CombatInput combatInput;
    [SerializeField] private TurnManager turnManager;

    [Header("Player Units (6)")]
    [SerializeField] private List<Unit> playerUnits = new();

    [Header("Deployment Zone (rectangle coords, inclusive)")]
    [SerializeField] private Vector2Int deployMin = new Vector2Int(0, 0);
    [SerializeField] private Vector2Int deployMax = new Vector2Int(3, 2);

    [Header("Placement Preview (tile cursor + ghost unit)")]
    [Tooltip("Prefab with children named 'Outline' and 'Ghost' (both SpriteRenderers).")]
    [SerializeField] private GameObject tilePreviewPrefab;

    [SerializeField] private Color previewValidColor = new Color(0.3f, 1f, 0.3f, 0.9f);
    [SerializeField] private Color previewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.9f);

    [Range(0f, 1f)]
    [SerializeField] private float ghostAlpha = 0.55f;

    private Unit selectedToPlace;

    private readonly HashSet<Tile> deployTiles = new();

    private bool deploymentActive = true;

    // Preview instance + renderers
    private GameObject tilePreviewInstance;
    private SpriteRenderer outlineSR;
    private SpriteRenderer ghostSR;

    private List<Unit> activePlayerUnits = new();

    [SerializeField] private VillagersDataSO villagersDataSO;

    VillagerData[] partyData = CampaignBattleData.partyVillagerData;
    bool[] partyCavalry = CampaignBattleData.partyCavalry;

    private void Awake()
    {
        // Combat UI off during deployment
        if (endTurnButton != null)
            endTurnButton.SetActive(false);

        // Start disabled until all placed
        if (startButton != null)
            startButton.interactable = false;

        // Ensure combat systems are disabled at start
        if (combatInput != null) combatInput.SetBattleStarted(false);
        if (turnManager != null) turnManager.SetBattleStarted(false);

        EnsurePreview();
        SetPreviewActive(false);
    }

    private void Start()
    {
        ApplyCampaignParty();
        RefreshStartButton();

        // Initial UI state (all not deployed)
        UpdateAllSlotDeployedStates();
        UpdateSlotSelection(null);
    }

    private void Update()
    {
        if (!deploymentActive) return;

        // ---- Preview follows pointer (mouse now; touch later) ----
        if (selectedToPlace != null)
        {
            Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            Tile tileUnderPointer = grid.GetTileFromWorld(worldPos);
            UpdatePreviewAt(tileUnderPointer);
        }
        else
        {
            SetPreviewActive(false);
        }

        // ---- Placement click ----
        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);

        // Prefer clicking a unit on board (pick up / reposition)
        // Only allow picking up a unit from the board if we are NOT already placing one
        if (selectedToPlace == null)
        {
            Collider2D[] hits = Physics2D.OverlapPointAll(world);
            if (hits != null && hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    Unit u = hits[i].GetComponentInParent<Unit>();
                    if (u != null && u.team == Unit.Team.Player && activePlayerUnits.Contains(u))
                    {
                        PickUpUnit(u);
                        return;
                    }
                }
            }
        }

        // Otherwise place on tile (use GridManager for reliability)
        Tile t = grid.GetTileFromWorld(world);
        if (t != null)
        {
            TryPlaceSelectedOnTile(t);
        }
    }

    public void RebuildDeploymentZone()
    {
        BuildDeployZone();
    }

    // -------------------- UI SLOT HOOK --------------------

    /// <summary>
    /// Call this from a UnitSlot button OnClick:
    /// Button -> DeploymentManager.SelectSlot(DeploymentUnitSlot)
    /// </summary>
    public void SelectSlot(DeploymentUnitSlot slot)
    {
        if (slot == null) return;
        SelectUnitToPlace(slot.unit);
    }

    // -------------------- UNIT SELECTION --------------------

    // Called from slots or elsewhere
    public void SelectUnitToPlace(Unit unit)
    {
        if (!deploymentActive) return;
        if (unit == null) return;
        if (!activePlayerUnits.Contains(unit)) return;

        bool isAlreadyPlaced =
            unit.CurrentTile != null &&
            deployTiles.Contains(unit.CurrentTile);

        if (isAlreadyPlaced)
        {
            PickUpUnit(unit);
            return;
        }

        selectedToPlace = unit;
        HighlightDeployZone(true);

        SetGhostSpriteFromUnit(unit);
        SetPreviewActive(true);

        UpdateSlotSelection(unit);
    }

    private void TryPlaceSelectedOnTile(Tile tile)
    {
        if (selectedToPlace == null) return;
        if (tile == null) return;

        // Must be in deploy zone
        if (!deployTiles.Contains(tile)) return;

        // Tile occupied by someone else? block
        if (tile.Occupant != null && tile.Occupant != selectedToPlace) return;

        // Place unit (Place already clears its previous tile occupancy)
        selectedToPlace.Place(tile);

        // Update UI slot deployed states
        UpdateAllSlotDeployedStates();

        // End placement for this unit (forces player to pick next unit)
        selectedToPlace = null;
        SetPreviewActive(false);
        UpdateSlotSelection(null);

        RefreshStartButton();
    }

    public void StartBattle()
    {
        if (!deploymentActive) return;
        if (!AllUnitsPlaced()) return;

        deploymentActive = false;

        HighlightDeployZone(false);

        selectedToPlace = null;
        SetPreviewActive(false);
        UpdateSlotSelection(null);

        foreach (var u in activePlayerUnits)
        {
            if (u == null) continue;
            u.HasMoved = false;
            u.HasActed = false;
            u.ResetGuard();
        }

        if (combatInput != null)
        {
            combatInput.SetBattleStarted(true);
            combatInput.ForceDeselect();
            combatInput.RefreshReadyHighlights();
        }

        if (turnManager != null)
        {
            turnManager.SetBattleStarted(true);
        }

        BattleManager.Instance?.StartBattle();

        // Hide deployment UI
        if (deploymentPanel != null)
            deploymentPanel.SetActive(false);

        // Enable combat UI
        if (endTurnButton != null)
            endTurnButton.SetActive(true);

        CombatCameraController.Instance?.CenterOnTileArea(
            grid,
            deployMin,
            deployMax,
            5f
        );
    }

    // -------------------- BUTTON / STATE --------------------

    private void RefreshStartButton()
    {
        if (startButton == null) return;
        startButton.interactable = AllUnitsPlaced();
    }

    private bool AllUnitsPlaced()
    {
        if (activePlayerUnits.Count == 0)
            return false;

        foreach (var u in activePlayerUnits)
        {
            if (u == null) return false;
            if (u.CurrentTile == null) return false;
            if (!deployTiles.Contains(u.CurrentTile)) return false;
        }

        return true;
    }

    private void BuildDeployZone()
    {
        deployTiles.Clear();

        for (int y = deployMin.y; y <= deployMax.y; y++)
            for (int x = deployMin.x; x <= deployMax.x; x++)
            {
                Tile t = grid.GetTile(new Vector2Int(x, y));
                if (t == null) continue;

                t.IsPlayerDeployTile = true;
                deployTiles.Add(t);
            }

        HighlightDeployZone(true);
    }

    private void HighlightDeployZone(bool on)
    {
        foreach (var t in deployTiles)
        {
            if (t == null) continue;
            t.SetDeployHighlight(on);
        }
    }

    // -------------------- SLOT UI HELPERS --------------------

    private void UpdateAllSlotDeployedStates()
    {
        if (unitSlots == null) return;

        foreach (var slot in unitSlots)
        {
            if (slot == null || slot.unit == null) continue;

            bool deployed = slot.unit.CurrentTile != null && deployTiles.Contains(slot.unit.CurrentTile);
            slot.SetDeployed(deployed);
        }
    }

    private void UpdateSlotSelection(Unit selected)
    {
        if (unitSlots == null) return;

        foreach (var slot in unitSlots)
        {
            if (slot == null) continue;
            slot.SetSelected(slot.unit == selected);
        }
    }

    // ---------------- Preview helpers ----------------

    private void EnsurePreview()
    {
        if (tilePreviewInstance != null) return;
        if (tilePreviewPrefab == null) return;

        tilePreviewInstance = Instantiate(tilePreviewPrefab);

        // Expect children:
        //  - Outline (SpriteRenderer)
        //  - Ghost   (SpriteRenderer)
        outlineSR = tilePreviewInstance.transform.Find("Outline")?.GetComponent<SpriteRenderer>();
        ghostSR = tilePreviewInstance.transform.Find("Ghost")?.GetComponent<SpriteRenderer>();

        if (outlineSR != null)
        {
            outlineSR.sortingLayerName = "Highlights";
            outlineSR.sortingOrder = 40;
        }

        if (ghostSR != null)
        {
            ghostSR.sortingLayerName = "Units";
            ghostSR.sortingOrder = 50;
        }

        tilePreviewInstance.SetActive(false);
    }

    private void SetPreviewActive(bool on)
    {
        EnsurePreview();
        if (tilePreviewInstance == null) return;

        tilePreviewInstance.SetActive(on);
    }

    private void UpdatePreviewAt(Tile tile)
    {
        EnsurePreview();
        if (tilePreviewInstance == null) return;

        if (selectedToPlace == null || tile == null)
        {
            tilePreviewInstance.SetActive(false);
            return;
        }

        tilePreviewInstance.SetActive(true);
        tilePreviewInstance.transform.position = tile.transform.position;

        bool valid =
            deployTiles.Contains(tile) &&
            (tile.Occupant == null || tile.Occupant == selectedToPlace);

        // Outline shows validity colors (preferred)
        if (outlineSR != null)
            outlineSR.color = valid ? previewValidColor : previewInvalidColor;

        // Ghost stays semi-transparent unit sprite
        if (ghostSR != null)
        {
            var c = ghostSR.color;
            c.a = ghostAlpha;
            ghostSR.color = c;
        }
    }

    private void SetGhostSpriteFromUnit(Unit unit)
    {
        EnsurePreview();
        if (ghostSR == null || unit == null) return;

        var unitSR = unit.GetComponent<SpriteRenderer>();
        if (unitSR == null) return;

        ghostSR.sprite = unitSR.sprite;
        ghostSR.flipX = unitSR.flipX;

        // Keep ghost semi-transparent while preserving its current RGB
        Color c = ghostSR.color;
        c.a = ghostAlpha;
        ghostSR.color = c;
    }

    private void PickUpUnit(Unit unit)
    {
        if (unit == null) return;

        unit.RemoveFromTile();
        unit.transform.position = new Vector3(9999f, 9999f, 0f);

        selectedToPlace = unit;

        SetGhostSpriteFromUnit(unit);
        SetPreviewActive(true);

        UpdateAllSlotDeployedStates();
        UpdateSlotSelection(unit);
        RefreshStartButton();
    }
    private void ApplyCampaignParty()
    {
        activePlayerUnits.Clear();

        VillagerData[] partyData = CampaignBattleData.partyVillagerData;
        bool[] partyCavalry = CampaignBattleData.partyCavalry;

        for (int i = 0; i < playerUnits.Count; i++)
        {
            if (playerUnits[i] != null)
                playerUnits[i].gameObject.SetActive(false);

            if (i < unitSlots.Count && unitSlots[i] != null)
                unitSlots[i].gameObject.SetActive(false);
        }

        if (partyData == null)
        {
            Debug.LogWarning("No campaign party data found.");
            return;
        }

        for (int i = 0; i < partyData.Length; i++)
        {
            VillagerData data = partyData[i];

            if (data == null)
                continue;

            if (activePlayerUnits.Count >= playerUnits.Count)
                break;

            int unitIndex = activePlayerUnits.Count;

            Unit unit = playerUnits[unitIndex];
            if (unit == null)
                continue;

            bool cavalry = partyCavalry != null &&
                           i < partyCavalry.Length &&
                           partyCavalry[i];

            unit.ApplyVillagerData(data, cavalry);

            activePlayerUnits.Add(unit);
            unit.gameObject.SetActive(true);

            if (unitIndex < unitSlots.Count && unitSlots[unitIndex] != null)
            {
                unitSlots[unitIndex].unit = unit;
                unitSlots[unitIndex].gameObject.SetActive(true);
                unitSlots[unitIndex].RefreshPortraitFromUnit();
                unitSlots[unitIndex].SetDeployed(false);
            }
        }
    }
    public void SetDeploymentZone(Vector2Int min, Vector2Int max)
    {
        deployMin = min;
        deployMax = max;
    }
}