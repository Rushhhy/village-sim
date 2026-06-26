using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public enum Phase { Player, Enemy }
    public Phase CurrentPhase { get; private set; } = Phase.Player;

    public bool IsPlayerPhase => CurrentPhase == Phase.Player;

    [SerializeField] private GridManager grid;
    [SerializeField] private CombatInput combatInput;
    [SerializeField] private BattleManager battleManager;

    [Header("End Turn UI")]
    [SerializeField] private Button endTurnButton;

    [Header("End Turn Pulse")]
    [SerializeField] private Vector3 endTurnBaseScale = new Vector3(2f, 2f, 2f);
    [SerializeField] private float pulseSpeed = 2.25f;
    [SerializeField] private float pulseScale = 1.06f;

    private readonly List<Unit> allUnits = new();

    [SerializeField] private bool battleStarted = false;
    private bool isBusy = false;

    private Coroutine pulseRoutine;

    public void SetBattleStarted(bool started)
    {
        battleStarted = started;
        UpdateEndTurnButtonState();
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;
        UpdateEndTurnButtonState();
    }

    private void Awake()
    {
        RefreshUnitList();
    }

    private void Start()
    {
        if (endTurnButton != null)
            endTurnButton.transform.localScale = endTurnBaseScale;

        UpdateEndTurnButtonState();
        StartCoroutine(InitialReadyRefresh());
    }

    private IEnumerator InitialReadyRefresh()
    {
        yield return null;

        if (combatInput != null)
            combatInput.RefreshReadyHighlights();
    }

    public void RefreshUnitList()
    {
        allUnits.Clear();

#if UNITY_2023_1_OR_NEWER
        allUnits.AddRange(FindObjectsByType<Unit>(FindObjectsSortMode.None));
#else
        allUnits.AddRange(FindObjectsOfType<Unit>());
#endif
    }

    public void EndTurn()
    {
        if (!battleStarted) return;
        if (isBusy) return;
        if (battleManager != null && battleManager.BattleOver) return;
        if (!IsPlayerPhase) return;

        if (combatInput != null)
            combatInput.ForceDeselect();

        CurrentPhase = Phase.Enemy;
        Debug.Log("Enemy Phase");

        UpdateEndTurnButtonState();

        if (combatInput != null)
            combatInput.RefreshReadyHighlights();

        StartCoroutine(EnemyPhaseRoutine());
    }

    private void ResetPlayerUnits()
    {
        RefreshUnitList();

        foreach (var u in allUnits)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Player) continue;

            u.HasMoved = false;
            u.HasActed = false;
            u.ResetGuard();
            u.TickSpecialCooldown();
        }
    }

    private void ResetEnemyUnits()
    {
        RefreshUnitList();

        foreach (var u in allUnits)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Enemy) continue;

            u.HasMoved = false;
            u.HasActed = false;
            u.ResetGuard();
            u.TickSpecialCooldown();
        }
    }

    private IEnumerator EnemyPhaseRoutine()
    {
        ResetEnemyUnits();
        RefreshUnitList();

        if (battleManager != null && battleManager.BattleOver)
        {
            UpdateEndTurnButtonState();
            yield break;
        }

        var players = new List<Unit>();
        var enemies = new List<Unit>();

        foreach (var u in allUnits)
        {
            if (u == null) continue;
            if (u.CurrentTile == null) continue;

            if (u.team == Unit.Team.Player) players.Add(u);
            else if (u.team == Unit.Team.Enemy) enemies.Add(u);
        }

        if (players.Count == 0)
        {
            CurrentPhase = Phase.Player;
            UpdateEndTurnButtonState();
            ResetPlayerUnits();

            if (battleManager != null)
                battleManager.CheckImmediateEndConditions();

            yield break;
        }

        foreach (var enemy in enemies)
        {
            if (battleManager != null && battleManager.BattleOver)
            {
                UpdateEndTurnButtonState();
                yield break;
            }

            if (enemy == null || enemy.CurrentTile == null || !enemy.IsAlive) continue;

            Unit target = FindNearestPlayer(enemy, players);
            if (target == null || target.CurrentTile == null || !target.IsAlive) continue;

            if (!enemy.HasActed)
            {
                bool canUseSpecialNow = enemy.CanUseSpecial && enemy.IsTargetInSpecialRange(target);
                bool canUseNormalNow = MovementService.IsInRange(enemy, target);

                if (canUseSpecialNow)
                {
                    yield return StartCoroutine(enemy.UseSpecialAttack(target));
                    enemy.HasActed = true;

                    if (battleManager != null && battleManager.BattleOver)
                    {
                        UpdateEndTurnButtonState();
                        yield break;
                    }
                }
                else if (canUseNormalNow)
                {
                    yield return StartCoroutine(enemy.AttackUnit(target));
                    enemy.HasActed = true;

                    if (battleManager != null && battleManager.BattleOver)
                    {
                        UpdateEndTurnButtonState();
                        yield break;
                    }
                }
                else
                {
                    if (!enemy.HasMoved)
                    {
                        var reachable = MovementService.ComputeReachable(enemy, grid);

                        Tile best;
                        if (enemy.attackRange > 1)
                        {
                            best = MovementService.ChooseBestRangedTile(
                                reachable,
                                enemy.CurrentTile,
                                target.CurrentTile,
                                enemy.attackRange
                            );
                        }
                        else
                        {
                            best = MovementService.ChooseBestTileTowards(
                                reachable,
                                enemy.CurrentTile,
                                target.CurrentTile
                            );
                        }

                        if (best != null && best != enemy.CurrentTile)
                        {
                            yield return StartCoroutine(enemy.MoveToTile(best));
                        }

                        enemy.HasMoved = true;
                    }

                    if (target != null && target.IsAlive && target.CurrentTile != null && !enemy.HasActed)
                    {
                        bool canUseSpecialAfterMove = enemy.CanUseSpecial && enemy.IsTargetInSpecialRange(target);
                        bool canUseNormalAfterMove = MovementService.IsInRange(enemy, target);

                        if (canUseSpecialAfterMove)
                        {
                            yield return StartCoroutine(enemy.UseSpecialAttack(target));
                            enemy.HasActed = true;

                            if (battleManager != null && battleManager.BattleOver)
                            {
                                UpdateEndTurnButtonState();
                                yield break;
                            }
                        }
                        else if (canUseNormalAfterMove)
                        {
                            yield return StartCoroutine(enemy.AttackUnit(target));
                            enemy.HasActed = true;

                            if (battleManager != null && battleManager.BattleOver)
                            {
                                UpdateEndTurnButtonState();
                                yield break;
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        CurrentPhase = Phase.Player;
        Debug.Log("Player Phase");

        if (battleManager != null)
        {
            battleManager.NotifyRoundAdvanced();

            if (battleManager.BattleOver)
            {
                UpdateEndTurnButtonState();
                yield break;
            }
        }

        ResetPlayerUnits();
        UpdateEndTurnButtonState();

        if (combatInput != null)
        {
            combatInput.ForceDeselect();
            combatInput.RefreshReadyHighlights();
        }
    }

    public void UpdateEndTurnButtonState()
    {
        bool canPress =
            battleStarted &&
            !isBusy &&
            CurrentPhase == Phase.Player &&
            (battleManager == null || !battleManager.BattleOver);

        if (endTurnButton != null)
            endTurnButton.interactable = canPress;

        if (!canPress)
        {
            StopPulse();
            return;
        }

        if (AnyPlayerUnitCanStillAct())
            StopPulse();
        else
            StartPulse();
    }

    private bool AnyPlayerUnitCanStillAct()
    {
        RefreshUnitList();

        foreach (var u in allUnits)
        {
            if (u == null) continue;
            if (u.team != Unit.Team.Player) continue;
            if (!u.IsAlive) continue;
            if (u.CurrentTile == null) continue;

            if (!u.HasMoved || !u.HasActed)
                return true;
        }

        return false;
    }

    private void StartPulse()
    {
        if (endTurnButton == null) return;
        if (pulseRoutine != null) return;

        pulseRoutine = StartCoroutine(PulseEndTurnRoutine());
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (endTurnButton != null)
            endTurnButton.transform.localScale = endTurnBaseScale;
    }

    private IEnumerator PulseEndTurnRoutine()
    {
        while (true)
        {
            float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;

            if (endTurnButton != null)
            {
                float scaleMul = Mathf.Lerp(1f, pulseScale, wave);
                endTurnButton.transform.localScale = endTurnBaseScale * scaleMul;
            }

            yield return null;
        }
    }

    private Unit FindNearestPlayer(Unit enemy, List<Unit> players)
    {
        Unit best = null;

        int bestGuardPriority = int.MinValue;
        int bestDist = int.MaxValue;

        foreach (var p in players)
        {
            if (p == null || p.CurrentTile == null || !p.IsAlive) continue;

            int dist = MovementService.Manhattan(enemy.CurrentTile.Coord, p.CurrentTile.Coord);

            int guardPriority = 0;

            if (p.IsGuardBroken)
                guardPriority = 2;
            else if (p.CurrentGuardStacks == 1)
                guardPriority = 1;

            if (guardPriority > bestGuardPriority)
            {
                bestGuardPriority = guardPriority;
                bestDist = dist;
                best = p;
            }
            else if (guardPriority == bestGuardPriority && dist < bestDist)
            {
                bestDist = dist;
                best = p;
            }
        }

        return best;
    }
}
