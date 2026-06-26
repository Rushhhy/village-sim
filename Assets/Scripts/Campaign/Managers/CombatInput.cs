using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CombatInput : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private GridManager grid;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private CombatPreviewUI previewUI;
    [SerializeField] private SpecialAbilityUI specialUI;
    [SerializeField] private AbilityConfirmUI abilityConfirmUI;

    [SerializeField] private GameObject selectionBoxPrefab;
    private GameObject activeSelectionBox;

    [SerializeField] private GameObject targetSelectionBoxPrefab;
    private GameObject activeTargetSelectionBox;

    private Unit selectedUnit;

    private HashSet<Tile> moveReachable = new();
    private readonly List<Unit> attackableEnemies = new();
    private readonly List<Tile> attackHighlightTiles = new();

    private readonly List<Tile> readyTiles = new();

    private Unit pendingTarget;

    private bool wasPlayerPhase = true;
    private bool isMoving = false;
    private bool specialMode = false;
    private bool abilityConfirmOpen = false;
    private bool attackConfirmOpen = false;

    [SerializeField] private bool battleStarted = false;

    public void SetBattleStarted(bool started)
    {
        battleStarted = started;

        if (!battleStarted)
        {
            Deselect();
            return;
        }

        RefreshSpecialUI();
    }

    void Update()
    {
        if (!battleStarted) return;

        bool isPlayerPhase = turnManager.IsPlayerPhase;

        if (wasPlayerPhase && !isPlayerPhase)
        {
            HardClearReadyHighlights();
            Deselect();
        }

        if (!isPlayerPhase || isMoving)
        {
            wasPlayerPhase = isPlayerPhase;
            return;
        }

        wasPlayerPhase = isPlayerPhase;

        // Ignore battlefield clicks when clicking UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!Input.GetMouseButtonDown(0)) return;

        Vector2 world = cam.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(world);
        if (hits == null || hits.Length == 0)
        {
            // Clicking empty space = deselect
            Deselect();
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Unit unit = hits[i].GetComponentInParent<Unit>();
            if (unit != null)
            {
                HandleUnitClick(unit);
                return;
            }
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Tile tile = hits[i].GetComponentInParent<Tile>();
            if (tile != null)
            {
                HandleTileClick(tile);
                return;
            }
        }

        // If somehow something else was hit, just deselect
        Deselect();
    }

    private void HandleUnitClick(Unit clicked)
    {
        if (clicked.team == Unit.Team.Player)
        {
            if (!CanSelectPlayerUnit(clicked))
                return;

            SelectPlayerUnit(clicked);
            return;
        }

        if (clicked.team == Unit.Team.Enemy && selectedUnit != null)
        {
            if (specialMode)
                TryOpenAbilityConfirm(clicked);
            else
                TryOpenAttackPreview(clicked);
        }
    }

    private void SelectPlayerUnit(Unit unit)
    {
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        pendingTarget = null;
        ClearTargetSelectionVisual();

        if (selectedUnit != unit)
            specialMode = false;

        selectedUnit = unit;

        UpdateSelectionVisual();
        RefreshSpecialUI();

        if (!selectedUnit.HasMoved)
            ShowMoveOptions();
        else
            ShowAttackOptions();

        RefreshSpecialUI();
    }

    private void HandleTileClick(Tile tile)
    {
        if (selectedUnit == null) return;

        bool isMoveTile = moveReachable.Contains(tile);
        bool isAttackTile = attackHighlightTiles.Contains(tile);

        // If a confirm panel is open and player clicks a tile instead of an enemy, deselect
        if (attackConfirmOpen || abilityConfirmOpen)
        {
            Deselect();
            return;
        }

        // Clicking any tile outside valid options deselects
        if (!isMoveTile && !isAttackTile)
        {
            Deselect();
            return;
        }

        // After moving / while acting, tile clicks should not do anything except deselect above
        if (selectedUnit.HasMoved || selectedUnit.HasActed) return;

        if (selectedUnit.CurrentTile != null && tile == selectedUnit.CurrentTile) return;

        if (!isMoveTile) return;

        StartCoroutine(MoveThenShowAttack(tile));
    }

    private IEnumerator MoveThenShowAttack(Tile tile)
    {
        if (selectedUnit == null) yield break;

        isMoving = true;
        if (turnManager != null) turnManager.SetBusy(true);

        Unit unit = selectedUnit;

        ClearAllHighlights();
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        pendingTarget = null;
        ClearTargetSelectionVisual();
        specialMode = false;

        if (unit.CurrentTile != null)
        {
            unit.CurrentTile.SetReadyHighlight(false);
            readyTiles.Remove(unit.CurrentTile);
        }

        RefreshSpecialUI();

        yield return StartCoroutine(unit.MoveToTile(tile));

        unit.HasMoved = true;

#if UNITY_2023_1_OR_NEWER
        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var units = Object.FindObjectsOfType<Unit>();
#endif

        bool hasAnyAttackInRange = false;

        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Enemy) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            bool canNormal = unit.IsTargetInNormalRange(u);
            bool canSpecial = unit.CanUseSpecial && unit.IsTargetInSpecialRange(u);

            if (canNormal || canSpecial)
            {
                hasAnyAttackInRange = true;
                break;
            }
        }

        if (!hasAnyAttackInRange)
        {
            unit.HasActed = true;

            RefreshReadyHighlights();
            RefreshSpecialUI();

            isMoving = false;
            if (turnManager != null) turnManager.SetBusy(false);

            Deselect();
            yield break;
        }

        if (selectedUnit == unit && !unit.HasActed)
        {
            ShowAttackOptions();
        }

        RefreshReadyHighlights();

        isMoving = false;
        if (turnManager != null) turnManager.SetBusy(false);

        RefreshSpecialUI();
    }

    private void ShowMoveOptions()
    {
        ClearAllHighlights();
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        pendingTarget = null;
        ClearTargetSelectionVisual();

        moveReachable = MovementService.ComputeReachable(selectedUnit, grid);

        if (selectedUnit.CurrentTile != null)
            moveReachable.Remove(selectedUnit.CurrentTile);

        foreach (var t in moveReachable)
        {
            if (t != null)
                t.SetHighlight(true);
        }

        RefreshAttackHighlights();
    }

    private void ShowAttackOptions()
    {
        ClearAllHighlights();
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        pendingTarget = null;
        ClearTargetSelectionVisual();

        RefreshAttackHighlights();
    }

    private void RefreshAttackHighlights()
    {
        ClearAttackHighlights();

        if (selectedUnit == null) return;

#if UNITY_2023_1_OR_NEWER
        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var units = Object.FindObjectsOfType<Unit>();
#endif
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Enemy) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            bool canAttack = specialMode
                ? selectedUnit.CanUseSpecial && selectedUnit.IsTargetInSpecialRange(u)
                : selectedUnit.IsTargetInNormalRange(u);

            if (canAttack)
            {
                attackableEnemies.Add(u);
                u.CurrentTile.SetAttackHighlight(true);
                attackHighlightTiles.Add(u.CurrentTile);
            }
        }
    }

    private void TryOpenAttackPreview(Unit target)
    {
        if (selectedUnit == null) return;
        if (selectedUnit.HasActed) return;

        if (!selectedUnit.IsTargetInNormalRange(target))
        {
            Debug.Log("Target not in normal range.");
            return;
        }

        pendingTarget = target;
        UpdateTargetSelectionVisual();

        attackConfirmOpen = true;
        abilityConfirmOpen = false;

        if (abilityConfirmUI != null)
            abilityConfirmUI.Hide();

        previewUI.Show(
            confirmAction: ConfirmAttack,
            cancelAction: OnAttackPreviewCancelled
        );
    }

    private void TryOpenAbilityConfirm(Unit target)
    {
        if (selectedUnit == null) return;
        if (selectedUnit.HasActed) return;
        if (!selectedUnit.CanUseSpecial) return;

        if (!selectedUnit.IsTargetInSpecialRange(target))
        {
            Debug.Log("Target not in special range.");
            return;
        }

        pendingTarget = target;
        UpdateTargetSelectionVisual();

        abilityConfirmOpen = true;
        attackConfirmOpen = false;

        previewUI.Hide();

        if (abilityConfirmUI != null)
        {
            abilityConfirmUI.Show(
                confirmAction: ConfirmAbility,
                cancelAction: OnAbilityConfirmCancelled
            );
        }
    }

    private void ConfirmAttack()
    {
        attackConfirmOpen = false;

        if (selectedUnit == null || pendingTarget == null)
        {
            previewUI.Hide();
            return;
        }

        if (selectedUnit.HasActed || pendingTarget.CurrentTile == null)
        {
            previewUI.Hide();
            return;
        }

        if (!selectedUnit.IsTargetInNormalRange(pendingTarget))
        {
            Debug.Log("Normal attack is no longer valid.");
            previewUI.Hide();
            return;
        }

        previewUI.Hide();

        if (activeSelectionBox != null)
        {
            Destroy(activeSelectionBox);
            activeSelectionBox = null;
        }

        ClearTargetSelectionVisual();

        StartCoroutine(AttackAndFinish(selectedUnit, pendingTarget));
    }

    private void ConfirmAbility()
    {
        abilityConfirmOpen = false;

        if (selectedUnit == null || pendingTarget == null)
        {
            if (abilityConfirmUI != null) abilityConfirmUI.Hide();
            return;
        }

        if (selectedUnit.HasActed || pendingTarget.CurrentTile == null)
        {
            if (abilityConfirmUI != null) abilityConfirmUI.Hide();
            return;
        }

        if (!selectedUnit.CanUseSpecial || !selectedUnit.IsTargetInSpecialRange(pendingTarget))
        {
            Debug.Log("Special is no longer valid.");
            if (abilityConfirmUI != null) abilityConfirmUI.Hide();
            return;
        }

        if (activeSelectionBox != null)
        {
            Destroy(activeSelectionBox);
            activeSelectionBox = null;
        }

        ClearTargetSelectionVisual();
        StartCoroutine(UseSpecialAndFinish(selectedUnit, pendingTarget));
    }

    private void OnAttackPreviewCancelled()
    {
        attackConfirmOpen = false;
        Deselect();
    }

    private void OnAbilityConfirmCancelled()
    {
        abilityConfirmOpen = false;
        Deselect();
    }

    private IEnumerator AttackAndFinish(Unit attacker, Unit target)
    {
        if (turnManager != null) turnManager.SetBusy(true);

        ClearAllHighlights();
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        pendingTarget = null;
        specialMode = false;
        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        RefreshSpecialUI();

        if (attacker.CurrentTile != null)
        {
            attacker.CurrentTile.SetReadyHighlight(false);
            readyTiles.Remove(attacker.CurrentTile);
        }

        if (activeTargetSelectionBox != null)
        {
            Destroy(activeTargetSelectionBox);
            activeTargetSelectionBox = null;
        }

        yield return StartCoroutine(attacker.AttackUnit(target));

        attacker.HasActed = true;
        RefreshReadyHighlights();

        Deselect();

        if (turnManager != null) turnManager.SetBusy(false);
    }

    private IEnumerator UseSpecialAndFinish(Unit attacker, Unit target)
    {
        if (turnManager != null) turnManager.SetBusy(true);

        ClearAllHighlights();
        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        pendingTarget = null;
        specialMode = false;
        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        if (attacker.CurrentTile != null)
        {
            attacker.CurrentTile.SetReadyHighlight(false);
            readyTiles.Remove(attacker.CurrentTile);
        }

        yield return StartCoroutine(attacker.UseSpecialAttack(target));

        attacker.HasActed = true;

        RefreshReadyHighlights();
        RefreshSpecialUI();

        Deselect();

        if (turnManager != null) turnManager.SetBusy(false);
    }

    public void OnSpecialButtonPressed()
    {
        if (selectedUnit == null) return;
        if (selectedUnit.HasActed) return;
        if (!selectedUnit.HasSpecialAttack) return;

        // allow enter or cancel
        if (!selectedUnit.CanUseSpecial && !specialMode) return;

        bool wasActive = specialMode;
        specialMode = !specialMode;

        // 🔥 Trigger pop ONLY when turning ON
        if (!wasActive && specialMode)
        {
            if (specialUI != null)
                specialUI.PlayButtonPop();
        }

        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        pendingTarget = null;
        ClearTargetSelectionVisual();

        if (!selectedUnit.HasMoved)
            ShowMoveOptions();
        else
            ShowAttackOptions();

        RefreshSpecialUI();
    }

    private void RefreshSpecialUI()
    {
        if (specialUI == null)
            return;

        if (selectedUnit == null)
        {
            specialUI.Hide();
            return;
        }

        specialUI.Show(selectedUnit, OnSpecialButtonPressed, specialMode);
    }

    public void ForceDeselect()
    {
        if (isMoving) return;
        Deselect();
    }

    private void Deselect()
    {
        if (isMoving) return;

        selectedUnit = null;
        pendingTarget = null;
        specialMode = false;
        attackConfirmOpen = false;
        abilityConfirmOpen = false;

        previewUI.Hide();
        if (abilityConfirmUI != null) abilityConfirmUI.Hide();

        ClearAllHighlights();

        if (activeSelectionBox != null)
        {
            Destroy(activeSelectionBox);
            activeSelectionBox = null;
        }

        ClearTargetSelectionVisual();

        if (specialUI != null)
            specialUI.Hide();
    }

    private void ClearAllHighlights()
    {
        ClearMoveHighlights();
        ClearAttackHighlights();
    }

    private void ClearMoveHighlights()
    {
        foreach (var t in moveReachable)
        {
            if (t != null) t.SetHighlight(false);
        }

        moveReachable.Clear();
    }

    private void ClearAttackHighlights()
    {
        foreach (var t in attackHighlightTiles)
        {
            if (t != null) t.SetAttackHighlight(false);
        }

        attackHighlightTiles.Clear();
        attackableEnemies.Clear();
    }

    private void HardClearReadyHighlights()
    {
#if UNITY_2023_1_OR_NEWER
        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var units = Object.FindObjectsOfType<Unit>();
#endif
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Player) continue;
            if (u.CurrentTile == null) continue;

            u.CurrentTile.SetReadyHighlight(false);
        }

        readyTiles.Clear();
    }

    public void RefreshReadyHighlights()
    {
        foreach (var t in readyTiles)
        {
            if (t != null) t.SetReadyHighlight(false);
        }
        readyTiles.Clear();

        if (!turnManager.IsPlayerPhase)
            return;

#if UNITY_2023_1_OR_NEWER
        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var units = Object.FindObjectsOfType<Unit>();
#endif

        var enemies = new List<Unit>();
        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Enemy) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            enemies.Add(u);
        }

        foreach (var u in units)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Player) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            if (u.HasActed) continue;

            bool canAct = false;

            if (!u.HasMoved)
            {
                canAct = true;
            }
            else
            {
                foreach (var enemy in enemies)
                {
                    bool canNormal = u.IsTargetInNormalRange(enemy);
                    bool canSpecial = u.CanUseSpecial && u.IsTargetInSpecialRange(enemy);

                    if (canNormal || canSpecial)
                    {
                        canAct = true;
                        break;
                    }
                }
            }

            if (canAct)
            {
                u.CurrentTile.SetReadyHighlight(true);
                readyTiles.Add(u.CurrentTile);
            }
        }
    }

    private void UpdateSelectionVisual()
    {
        if (selectedUnit == null)
        {
            if (activeSelectionBox != null)
            {
                Destroy(activeSelectionBox);
                activeSelectionBox = null;
            }
            return;
        }

        if (selectionBoxPrefab == null)
            return;

        if (activeSelectionBox == null)
        {
            activeSelectionBox = Instantiate(
                selectionBoxPrefab,
                selectedUnit.transform
            );
            activeSelectionBox.transform.localPosition = Vector3.zero;
        }
        else
        {
            if (activeSelectionBox.transform.parent != selectedUnit.transform)
            {
                activeSelectionBox.transform.SetParent(selectedUnit.transform);
                activeSelectionBox.transform.localPosition = Vector3.zero;
            }
        }
    }

    private void UpdateTargetSelectionVisual()
    {
        if (pendingTarget == null)
        {
            if (activeTargetSelectionBox != null)
            {
                Destroy(activeTargetSelectionBox);
                activeTargetSelectionBox = null;
            }
            return;
        }

        if (targetSelectionBoxPrefab == null)
            return;

        if (activeTargetSelectionBox == null)
        {
            activeTargetSelectionBox = Instantiate(
                targetSelectionBoxPrefab,
                pendingTarget.transform
            );
            activeTargetSelectionBox.transform.localPosition = Vector3.zero;
        }
        else
        {
            if (activeTargetSelectionBox.transform.parent != pendingTarget.transform)
            {
                activeTargetSelectionBox.transform.SetParent(pendingTarget.transform);
            }
            activeTargetSelectionBox.transform.localPosition = Vector3.zero;
        }
    }

    private void ClearTargetSelectionVisual()
    {
        if (activeTargetSelectionBox != null)
        {
            Destroy(activeTargetSelectionBox);
            activeTargetSelectionBox = null;
        }
    }

    private bool CanSelectPlayerUnit(Unit u)
    {
        if (u == null) return false;
        if (u.team != Unit.Team.Player) return false;
        if (!u.IsAlive) return false;
        if (u.CurrentTile == null) return false;
        if (u.HasActed) return false;

        if (!u.HasMoved) return true;

#if UNITY_2023_1_OR_NEWER
        var units = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var units = Object.FindObjectsOfType<Unit>();
#endif
        foreach (var enemy in units)
        {
            if (enemy == null) continue;
            if (enemy.team != Unit.Team.Enemy) continue;
            if (!enemy.IsAlive) continue;
            if (enemy.CurrentTile == null) continue;

            bool canNormal = u.IsTargetInNormalRange(enemy);
            bool canSpecial = u.CanUseSpecial && u.IsTargetInSpecialRange(enemy);

            if (canNormal || canSpecial)
                return true;
        }

        return false;
    }
}