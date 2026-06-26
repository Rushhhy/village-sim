using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransportingBehavior : MonoBehaviour
{
    [SerializeField] private WorkBehaviorSettings settings;

    private WorkBehavior workBehavior;

    private IWorkBehaviorTarget target;
    private AStarPathfinder pathfinder;
    private Coroutine transportingCoroutine;
    private Coroutine movementCoroutine;

    // Dependencies
    private Vector3 buildingPos;
    private List<Vector3> workPos;
    private int currentBuildingID;

    // Current path and movement state
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;

    public bool IsTransporting => transportingCoroutine != null;

    // Events
    public System.Action OnWorkPositionAvailable;

    private void Awake()
    {
        target = GetComponent<IWorkBehaviorTarget>();
        if (target == null)
        {
            Debug.LogError($"TransportingBehavior requires a component that implements IWorkBehaviorTarget on {gameObject.name}");
        }

        workBehavior = GetComponent<WorkBehavior>();
    }

    public void Initialize(WorkBehaviorSettings behaviorSettings, GridData gridData)
    {
        settings = behaviorSettings;

        // Create A* pathfinder
        if (gridData != null && settings.pathfindingSettings != null)
        {
            pathfinder = new AStarPathfinder(gridData, settings.pathfindingSettings, gameObject.name);
        }
        else
        {
            Debug.LogError($"Cannot initialize pathfinder for {gameObject.name} - missing GridData or PathfindingSettings");
        }
    }

    public void SetBuildingData(int buildingID, Vector3 buildingPosition, List<Vector3> workPositions)
    {
        currentBuildingID = buildingID;
        buildingPos = buildingPosition + new Vector3(1, 0, 0);
        workPos = workPositions;
    }

    public void StartTransporting()
    {
        Debug.Log($"StartTransporting called for {gameObject.name}");

        if (transportingCoroutine != null)
        {
            StopTransporting();
        }

        transportingCoroutine = StartCoroutine(TransportingCoroutine());
        Debug.Log($"TransportingCoroutine started for {gameObject.name}");
    }

    public void StopTransporting()
    {
        if (transportingCoroutine != null)
        {
            StopCoroutine(transportingCoroutine);
            transportingCoroutine = null;
        }

        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }

        isMoving = false;
        currentPath = null;
        currentPathIndex = 0;
    }

    private IEnumerator TransportingCoroutine()
    {
        yield return null;

        yield return StartCoroutine(RepositionToValidPosition());

        float maxTransportTime = 20f;
        float elapsed = 0f;

        while (IsTransporting && elapsed < maxTransportTime)
        {
            elapsed += Time.deltaTime;

            Vector3 marketPos = GetRandomAvailableMarketPosition();

            yield return StartCoroutine(MoveToPositionWithPathfinding(marketPos, settings.boxWalkAnimationName));

            if (!IsTransporting) yield break;

            yield return StartCoroutine(MoveToPositionWithPathfinding(buildingPos, settings.walkAnimationName));

            if (!IsTransporting) yield break;

            if (currentBuildingID == 26) // Logger
            {
                OnWorkPositionAvailable?.Invoke();
                yield break;
            }

            if (HasAvailableWorkPosition())
            {
                OnWorkPositionAvailable?.Invoke();
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }

        // fallback if stuck too long
        if (IsTransporting)
        {
            Debug.LogWarning($"{gameObject.name} stuck transporting too long, resetting work state");
            OnWorkPositionAvailable?.Invoke(); // force re-evaluation
        }
    }

    private IEnumerator MoveToPositionWithPathfinding(Vector3 targetPosition, string animationName)
    {
        // Play animation
        if (target.Animator != null)
        {
            target.Animator.Play(animationName);
        }

        // If no pathfinder, fall back to simple movement
        if (pathfinder == null)
        {
            Debug.LogWarning($"No pathfinder available for {gameObject.name}, using simple movement");
            yield return StartCoroutine(SimpleMovement(targetPosition));
            yield break;
        }

        // Find path using A*
        Vector3 startPos = target.Transform.position;
        currentPath = pathfinder.FindPath(startPos, targetPosition);

        if (currentPath == null || currentPath.Count == 0)
        {
            currentPath = pathfinder.FindPathToArea(startPos, targetPosition, 3f);

            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.LogWarning($"No path found for {gameObject.name}, trying to reposition near building");

                yield return StartCoroutine(RepositionToValidPosition());

                currentPath = pathfinder.FindPath(target.Transform.position, targetPosition);

                if (currentPath == null || currentPath.Count == 0)
                {
                    Debug.LogWarning($"Still no valid path for {gameObject.name}, skipping this transport trip");
                    yield break;
                }
            }
        }

        movementCoroutine = StartCoroutine(FollowPath());
        yield return movementCoroutine;
        movementCoroutine = null;
    }

    private IEnumerator FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.LogWarning($"No path to follow for {gameObject.name}");
            yield break;
        }

        isMoving = true;
        currentPathIndex = 0;

        // Start from the first waypoint (skip if we're already very close to it)
        if (Vector3.Distance(target.Transform.position, currentPath[0]) < 0.1f)
        {
            currentPathIndex = 1;
        }

        while (isMoving && currentPath != null && currentPathIndex < currentPath.Count)
        {
            Vector3 currentTarget = currentPath[currentPathIndex];

            // Move towards current waypoint
            while (isMoving && currentPath != null && Vector3.Distance(target.Transform.position, currentTarget) > 0.1f)
            {
                // Check if current position is still walkable
                if (!pathfinder.IsPositionWalkable(pathfinder.WorldToGridPosition(target.Transform.position)))
                {
                    yield return StartCoroutine(RepositionToValidPosition());
                    yield break;
                }

                // Move towards waypoint
                Vector3 direction = (currentTarget - target.Transform.position).normalized;
                Vector3 newPosition = target.Transform.position + direction * settings.movementSpeed * Time.deltaTime;

                // Check if new position would be walkable
                if (pathfinder.IsPositionWalkable(pathfinder.WorldToGridPosition(newPosition)))
                {
                    target.Transform.position = newPosition;
                }
                else
                {
                    // Recalculate path from current position
                    Vector3 finalTarget = currentPath[currentPath.Count - 1];
                    currentPath = pathfinder.FindPath(target.Transform.position, finalTarget);
                    if (currentPath == null)
                    {
                        yield return StartCoroutine(RepositionToValidPosition());
                        yield break;
                    }
                    currentPathIndex = 0;
                    break;
                }

                // Handle sprite flipping
                if (settings.enableSpriteFlipping)
                {
                    HandleSpriteFlipping(direction);
                }

                // Update sorting order based on position
                if (target.SpriteRenderer != null)
                {
                    target.SpriteRenderer.sortingOrder = Mathf.RoundToInt(-target.Transform.position.y * 10);
                }

                yield return null;
            }

            // Move to next waypoint
            currentPathIndex++;
        }

        isMoving = false;
        currentPath = null;
        currentPathIndex = 0;
    }

    private IEnumerator SimpleMovement(Vector3 targetPosition)
    {
        while (Vector3.Distance(target.Transform.position, targetPosition) > 0.1f)
        {
            Vector3 direction = (targetPosition - target.Transform.position).normalized;
            target.Transform.position = Vector3.MoveTowards(
                target.Transform.position,
                targetPosition,
                settings.movementSpeed * Time.deltaTime
            );

            if (settings.enableSpriteFlipping)
            {
                HandleSpriteFlipping(direction);
            }

            // Update sorting order
            if (target.SpriteRenderer != null)
            {
                target.SpriteRenderer.sortingOrder = Mathf.RoundToInt(-target.Transform.position.y * 10);
            }

            yield return null;
        }
    }

    private IEnumerator RepositionToValidPosition()
    {
        if (pathfinder == null)
        {
            yield break;
        }

        Vector3 currentPos = target.Transform.position;
        Vector3Int gridPos = pathfinder.WorldToGridPosition(currentPos);

        if (!pathfinder.IsPositionWalkable(gridPos))
        {
            // Instead of searching from current position, search from building position
            Vector3Int buildingGridPos = pathfinder.WorldToGridPosition(buildingPos);

            // Try to find a walkable position near the building
            for (int radius = 1; radius <= 5; radius++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(y) != radius) continue; // Only check perimeter

                        Vector3Int testPos = buildingGridPos + new Vector3Int(x, y, 0);
                        if (pathfinder.IsPositionWalkable(testPos))
                        {
                            Vector3 newWorldPos = pathfinder.GridToWorldPosition(testPos);
                            target.Transform.position = newWorldPos;
                            yield break;
                        }
                    }
                }
            }

            Debug.LogError($"Could not find valid position near building for {gameObject.name}");
        }
    }

    private bool HasAvailableWorkPosition()
    {
        return workBehavior != null && workBehavior.CheckWorkPosAvailability().HasValue;
    }

    private Vector3 GetRandomAvailableMarketPosition()
    {
        List<Vector3> availableMarkets = new List<Vector3>
        {
            settings.marketPositionOne,
            settings.marketPositionTwo,
            settings.marketPositionThree,
            settings.marketPositionFour
        };

        return availableMarkets[Random.Range(0, availableMarkets.Count)];
    }

    private void HandleSpriteFlipping(Vector3 direction)
    {
        if (target.SpriteRenderer == null || Mathf.Abs(direction.x) < 0.1f) return;

        // Since animations naturally face left, flip when moving right
        var workBehavior = GetComponent<WorkBehavior>();

        if (workBehavior != null && workBehavior.IsForcingFacing())
        {
            return;
        }

        target.SpriteRenderer.flipX = direction.x > 0;
    }

    private void OnDestroy()
    {
        StopTransporting();
    }

    private void OnDrawGizmosSelected()
    {
        // Debug visualization
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }

            // Highlight current target
            if (currentPathIndex < currentPath.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(currentPath[currentPathIndex], 0.3f);
            }
        }
    }
}