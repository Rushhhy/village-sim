using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorkState
{
    None,
    Bathing,
    Transporting,
    Training,
    Logging,
    Producing,
    Watering,
    Mining
}

[System.Serializable]
public class WorkBehaviorSettings
{
    [Header("Movement Settings")]
    public float movementSpeed = 1f;

    [Header("Work Settings")]
    public float workInterval = 10f;
    public float workDuration = 30f; // Duration for Mining, Producing, Watering

    [Header("Animation Settings")]
    public string bathingAnimationName = "Swim";
    public string walkAnimationName = "Walk";
    public string boxWalkAnimationName = "Box";
    public string waterAnimationName = "Water";
    public string axeAnimationName = "Axe";
    public string axeWalkAnimationName = "Axe Walk";
    public string attackAnimationName = "Attack";
    public string workAnimationName = "Work";
    public string mineAnimationName = "Mine";

    [Header("Market Positions")]
    public Vector3 marketPositionOne = new Vector3(-2.73f, 29f, 0f);
    public Vector3 marketPositionTwo = new Vector3(-1.355f, 29f, 0f);
    public Vector3 marketPositionThree = new Vector3(4.2f, 29f, 0f);
    public Vector3 marketPositionFour = new Vector3(5.65f, 29f, 0f);

    [Header("Sprite Flipping")]
    public bool enableSpriteFlipping = true;
    public bool isLeftFacingDefault = true;

    [Header("Pathfinding")]
    public PathfindingSettings pathfindingSettings;

}

public interface IWorkBehaviorTarget
{
    Transform Transform { get; }
    Animator Animator { get; }
    SpriteRenderer SpriteRenderer { get; }
}

public class WorkBehavior : MonoBehaviour
{
    private WorkState workState;
    private List<Vector3> workPos;
    private Vector3 buildingPos;
    private int currentBuildingID;

    [SerializeField] private GridData gridData;
    [SerializeField] public WorkBehaviorSettings settings;

    private IWorkBehaviorTarget target;
    private GridPathfinder pathfinder;

    // Specialized behavior components
    private LoggingBehavior loggingBehavior;
    private TransportingBehavior transportingBehavior;

    // Remaining coroutines for non-separated behaviors
    private Coroutine producingCoroutine;
    private Coroutine bathingCoroutine;
    private Coroutine trainingCoroutine;
    private Coroutine wateringCoroutine;
    private Coroutine miningCoroutine;

    private bool forceFacingDirection = false;
    private bool faceRight = false;

    private void Awake()
    {
        target = GetComponent<IWorkBehaviorTarget>();
        if (target == null)
        {
            Debug.LogError($"WorkBehavior requires a component that implements IWorkBehaviorTarget on {gameObject.name}");
        }

        // Find GridData if not assigned
        if (gridData == null)
        {
            var placementSystem = GameObject.Find("PlacementSystem")?.GetComponent<PlacementSystem>();
            if (placementSystem != null)
            {
                gridData = placementSystem.gridData;
            }

            if (gridData == null)
            {
                Debug.LogError($"GridData not found for WorkBehavior on {gameObject.name}");
            }
        }

        // Initialize pathfinder
        if (gridData != null && settings != null && settings.pathfindingSettings != null)
        {
            pathfinder = new GridPathfinder(gridData, settings.pathfindingSettings, gameObject.name);
        }

        // Initialize specialized behaviors
        InitializeSpecializedBehaviors();
    }

    private void InitializeSpecializedBehaviors()
    {
        // Add or get LoggingBehavior component
        loggingBehavior = GetComponent<LoggingBehavior>();
        if (loggingBehavior == null)
        {
            loggingBehavior = gameObject.AddComponent<LoggingBehavior>();
        }
        if (loggingBehavior != null)
        {
            loggingBehavior.Initialize(settings, gridData);
        }

        // Add or get TransportingBehavior component
        transportingBehavior = GetComponent<TransportingBehavior>();
        if (transportingBehavior == null)
        {
            transportingBehavior = gameObject.AddComponent<TransportingBehavior>();
        }
        if (transportingBehavior != null)
        {
            transportingBehavior.Initialize(settings, gridData);
            transportingBehavior.OnWorkPositionAvailable -= OnWorkPositionBecameAvailable;
            transportingBehavior.OnWorkPositionAvailable += OnWorkPositionBecameAvailable;
        }
    }

    private void OnWorkPositionBecameAvailable()
    {
        SwitchToWorkState();
    }

    private void ResetSpriteFlip()
    {
        if (target?.SpriteRenderer != null)
        {
            target.SpriteRenderer.flipX = false; // Reset flipX to default left-facing
        }

        if (target?.Transform != null)
        {
            Vector3 scale = target.Transform.localScale;
            target.Transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        }
    }

    public void StartWorking(int buildingID, Vector3 buildingPosition, List<Vector3> workPositions)
    {
        // Stop any existing work FIRST
        StopAllWork();

        currentBuildingID = buildingID;
        workPos = workPositions;
        buildingPos = buildingPosition;

        transportingBehavior.SetBuildingData(buildingID, buildingPosition, workPositions);

        if (loggingBehavior != null)
        {
            loggingBehavior.SetBuildingPosition(buildingPosition);
        }

        // Determine initial work state based on building ID
        switch (buildingID)
        {
            case 19: // Barracks
                StartTraining();
                break;

            case 20: // Bath House
                StartBathing();
                break;

            case 22: // Carpenter
            case 27: // Lumber Mill
            case 34: // Tailor
            case 37: // Workshop
                if (HasAvailableWorkPosition())
                {
                    StartProducing();
                }
                else
                {
                    StartTransporting();
                }
                break;

            case 23: // Farm
                if (HasAvailableWorkPosition())
                {
                    StartWatering();
                }
                else
                {
                    StartTransporting();
                }
                break;

            case 26: // Logger
                StartLogging();
                break;

            case 28: // Mine
                if (HasAvailableWorkPosition())
                {
                    StartMining();
                }
                else
                {
                    StartTransporting();
                }
                break;

            case 24: // Hospital
                SetWorkStateToNone(false);
                break;

            default:
                StartTransporting();
                break;
        }
    }

    #region Start/Stop Methods

    public void StartTraining()
    {
        ResetSpriteFlip();

        Vector3? availablePos = CheckWorkPosAvailability();
        if (availablePos.HasValue)
        {
            workState = WorkState.Training;
            target.SpriteRenderer.sortingOrder = 0;
            trainingCoroutine = StartCoroutine(TrainingCoroutine(availablePos.Value));
        }
        else
        {
            SetWorkStateToNone(false);
        }
    }

    public void StopTraining()
    {
        if (trainingCoroutine != null)
        {
            StopCoroutine(trainingCoroutine);
            trainingCoroutine = null;
        }
    }

    public bool IsWorking()
    {
        return workState != WorkState.None;
    }

    public void StartBathing()
    {
        ResetSpriteFlip();

        Vector3? availablePos = CheckWorkPosAvailability();
        if (availablePos.HasValue)
        {
            workState = WorkState.Bathing;
            target.SpriteRenderer.sortingOrder = 0;
            bathingCoroutine = StartCoroutine(BathingCoroutine(availablePos.Value));
        }
        else
        {
            SetWorkStateToNone(false);
        }
    }

    public void StopBathing()
    {
        if (bathingCoroutine != null)
        {
            StopCoroutine(bathingCoroutine);
            bathingCoroutine = null;
        }
    }

    public void StartTransporting()
    {
        if (transportingBehavior == null)
        {
            Debug.LogError($"TransportingBehavior missing on {gameObject.name}");
            return;
        }

        ResetSpriteFlip();
        workState = WorkState.Transporting;
        transportingBehavior.StartTransporting();
    }
    public void StopTransporting()
    {
        if (transportingBehavior != null)
        {
            transportingBehavior.StopTransporting();
        }
    }

    public void StartProducing()
    {
        ResetSpriteFlip();

        Vector3? availablePos = CheckWorkPosAvailability();
        if (availablePos.HasValue)
        {
            workState = WorkState.Producing;
            target.SpriteRenderer.sortingOrder = 0;
            producingCoroutine = StartCoroutine(ProducingCoroutine(availablePos.Value));
        }
        else
        {
            StartTransporting();
        }
    }

    public void StopProducing()
    {
        if (producingCoroutine != null)
        {
            StopCoroutine(producingCoroutine);
            producingCoroutine = null;
        }
    }

    public void StartWatering()
    {
        ResetSpriteFlip();

        Vector3? availablePos = CheckWorkPosAvailability();
        if (availablePos.HasValue)
        {
            workState = WorkState.Watering;
            target.SpriteRenderer.sortingOrder = 0;
            wateringCoroutine = StartCoroutine(WateringCoroutine(availablePos.Value));
        }
        else
        {
            StartTransporting();
        }
    }

    public void StartMining()
    {
        ResetSpriteFlip();

        Vector3? availablePos = CheckWorkPosAvailability();
        if (availablePos.HasValue)
        {
            workState = WorkState.Mining;
            target.SpriteRenderer.sortingOrder = 0;
            miningCoroutine = StartCoroutine(MiningCoroutine(availablePos.Value));
        }
        else
        {
            StartTransporting();
        }
    }

    public void StopWatering()
    {
        if (wateringCoroutine != null)
        {
            StopCoroutine(wateringCoroutine);
            wateringCoroutine = null;
        }
    }

    public void StopMining()
    {
        if (miningCoroutine != null)
        {
            StopCoroutine(miningCoroutine);
            miningCoroutine = null;
        }
    }

    public void StartLogging()
    {
        if (loggingBehavior == null)
        {
            Debug.LogError($"LoggingBehavior missing on {gameObject.name}");
            return;
        }

        ResetSpriteFlip();
        workState = WorkState.Logging;
        loggingBehavior.StartLogging();
    }

    public void StopLogging()
    {
        if (loggingBehavior != null)
        {
            loggingBehavior.StopLogging();
        }
    }

    #endregion

    #region Remaining Coroutines

    private IEnumerator TrainingCoroutine(Vector3 workPosition)
    {
        yield return null;

        Vector3 targetPos = buildingPos + workPosition;

        // Teleport to work position
        target.Transform.position = targetPos;

        // Start training animation
        target.Animator.Play(settings.attackAnimationName);

        // Training continues indefinitely until stopped
        while (workState == WorkState.Training)
        {
            yield return new WaitForSeconds(settings.workInterval);
        }
    }

    private IEnumerator BathingCoroutine(Vector3 workPosition)
    {
        yield return null;

        Vector3 targetPos = buildingPos + workPosition;

        // Teleport to work position
        target.Transform.position = targetPos;

        // Start bathing animation
        target.Animator.Play(settings.bathingAnimationName);

        // Bathing continues indefinitely until stopped
        while (workState == WorkState.Bathing)
        {
            yield return new WaitForSeconds(settings.workInterval);
        }
    }

    private IEnumerator ProducingCoroutine(Vector3 workPosition)
    {
        yield return null;

        Vector3 targetPos = buildingPos + workPosition;

        target.Transform.position = targetPos;

        target.Animator.Play(settings.workAnimationName);

        yield return new WaitForSeconds(settings.workDuration);

        if (workState == WorkState.Producing)
        {
            StartTransporting();
        }
    }

    private IEnumerator WateringCoroutine(Vector3 workPosition)
    {
        yield return null;

        Vector3 targetPos = buildingPos + workPosition;

        target.Transform.position = targetPos;

        target.Animator.Play(settings.waterAnimationName);

        yield return new WaitForSeconds(settings.workDuration);

        if (workState == WorkState.Watering)
        {
            StartTransporting();
        }
    }

    private IEnumerator MiningCoroutine(Vector3 workPosition)
    {
        yield return null;

        Vector3 targetPos = buildingPos + workPosition;

        target.Transform.position = targetPos;

        // 👉 Check if it's the left mining spot
        if (workPosition.x == 0)
        {
            forceFacingDirection = true;
            faceRight = true; // adjust if needed visually
            SetFacing(faceRight);
        }

        target.Animator.Play(settings.mineAnimationName);

        yield return new WaitForSeconds(settings.workDuration);

        forceFacingDirection = false;

        if (workState == WorkState.Mining)
        {
            StartTransporting();
        }
    }

    #endregion

    #region Helper Methods

    public Vector3? CheckWorkPosAvailability()
    {
        if (workPos == null || workPos.Count == 0) return null;

        bool loopForward = UnityEngine.Random.Range(0, 2) == 0;

        if (loopForward)
        {
            for (int i = 0; i < workPos.Count; i++)
            {
                Vector3 checkPosition = buildingPos + workPos[i];
                Collider2D hit = Physics2D.OverlapPoint(checkPosition);

                if (hit == null || hit.GetComponent<Villager>() == null)
                {
                    return workPos[i];
                }
            }
        }
        else
        {
            for (int i = workPos.Count - 1; i >= 0; i--)
            {
                Vector3 checkPosition = buildingPos + workPos[i];
                Collider2D hit = Physics2D.OverlapPoint(checkPosition);

                if (hit == null || hit.GetComponent<Villager>() == null)
                {
                    return workPos[i];
                }
            }
        }

        return null;
    }

    private bool HasAvailableWorkPosition()
    {
        return CheckWorkPosAvailability().HasValue;
    }

    private void SwitchToWorkState()
    {
        StopTransporting();

        switch (currentBuildingID)
        {
            case 22: // Carpenter
            case 26: // Logger
                StartLogging();
                break;
            case 27: // Lumber Mill
            case 34: // Tailor
            case 37: // Workshop
                StartProducing();
                break;

            case 23: // Farm
                StartWatering();
                break;

            case 28: // Mine
                StartMining();
                break;

            default:
                SetWorkStateToNone(false);
                break;
        }
    }

    private void StopAllWork()
    {
        StopTraining();
        StopBathing();
        StopTransporting();
        StopProducing();
        StopWatering();
        StopMining();
        StopLogging();

        workState = WorkState.None;
    }

    #endregion

    #region Public Methods

    public void SetBuildingPos(Vector3 newBuildingPos)
    {
        buildingPos = newBuildingPos;

        // Update transporting behavior with new building position
        if (transportingBehavior != null)
        {
            transportingBehavior.SetBuildingData(currentBuildingID, newBuildingPos, workPos);
        }
    }

    public WorkState GetCurrentWorkState()
    {
        return workState;
    }

    public void SetWorkStateToNone(bool resetPosition = false)
    {
        StopAllWork();
        ResetSpriteFlip();

        if (resetPosition && target?.Transform != null)
        {
            target.Transform.position = Vector3.zero;
        }

        if (target?.Animator != null)
        {
            target.Animator.Play("Idle");
        }
    }

    public void StopWorking()
    {
        SetWorkStateToNone(false);
    }


    #endregion

    private void OnDestroy()
    {
        if (transportingBehavior != null)
        {
            transportingBehavior.OnWorkPositionAvailable -= OnWorkPositionBecameAvailable;
        }

        StopAllWork();
    }
    public void InitializeWork(int buildingID, Vector3 buildingPosition, List<Vector3> workPositions)
    {
        StartWorking(buildingID, buildingPosition, workPositions);
    }

    private void MoveInstantlyToValidPosition(Vector3 targetPos)
    {
        if (pathfinder == null)
        {
            target.Transform.position = targetPos;
            return;
        }

        Vector3Int gridPos = pathfinder.WorldToGridPosition(targetPos);

        if (pathfinder.IsPositionWalkable(gridPos))
        {
            target.Transform.position = targetPos;
        }
        else
        {
            Vector3? newPos = pathfinder.FindNearestWalkablePosition(targetPos);
            if (newPos.HasValue)
            {
                target.Transform.position = newPos.Value;
            }
            else
            {
                Debug.LogWarning($"No valid position found near {targetPos} for {gameObject.name}");
            }
        }
    }
    private void SetFacing(bool shouldFaceRight)
    {
        if (target?.SpriteRenderer == null) return;

        target.SpriteRenderer.flipX = shouldFaceRight;
    }

    public bool IsForcingFacing()
    {
        return forceFacingDirection;
    }
}