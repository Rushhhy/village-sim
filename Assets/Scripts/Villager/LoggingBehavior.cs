using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoggingBehavior : MonoBehaviour
{
    [SerializeField] private WorkBehaviorSettings settings;

    private IWorkBehaviorTarget target;
    private AStarPathfinder pathfinder;
    private Coroutine loggingCoroutine;

    private Vector3 buildingPos;

    private float treeSearchRadius = 8f;

    [SerializeField] private Vector3 loggingTreeOffset = new Vector3(0, -0.25f, 0f);

    public bool IsLogging => loggingCoroutine != null;

    private void Awake()
    {
        target = GetComponent<IWorkBehaviorTarget>();
        if (target == null)
        {
            Debug.LogError($"LoggingBehavior requires a component that implements IWorkBehaviorTarget on {gameObject.name}");
        }
    }

    public void Initialize(WorkBehaviorSettings behaviorSettings, GridData gridData)
    {
        settings = behaviorSettings;
        pathfinder = new AStarPathfinder(gridData, settings.pathfindingSettings, gameObject.name);
    }

    public void StartLogging()
    {
        if (loggingCoroutine != null)
        {
            StopLogging();
        }

        loggingCoroutine = StartCoroutine(LoggingCoroutine());
    }

    public void StopLogging()
    {
        if (loggingCoroutine != null)
        {
            StopCoroutine(loggingCoroutine);
            loggingCoroutine = null;
        }
    }

    public void SetBuildingPosition(Vector3 position)
    {
        buildingPos = position;
    }

    private IEnumerator LoggingCoroutine()
    {
        yield return null;

        while (true)
        {
            GameObject tree = FindRandomTreeInRadius();

            if (tree == null)
            {
                Debug.LogWarning($"No tree found near {gameObject.name}");
                yield return new WaitForSeconds(1f);
                continue;
            }

            yield return StartCoroutine(MoveToPosition(tree.transform.position + loggingTreeOffset, settings.axeWalkAnimationName));

            if (!IsLogging) yield break;

            target.Animator.Play(settings.axeAnimationName);

            TreeChopFeedback feedback = tree.GetComponent<TreeChopFeedback>();

            float elapsed = 0f;
            float interval = 0.6f; // match roughly your axe animation rhythm

            while (elapsed < settings.workDuration)
            {
                if (!IsLogging) yield break;

                if (feedback != null)
                {
                    feedback.PlayChopFeedback();
                }

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            StopLogging();

            WorkBehavior workBehavior = GetComponent<WorkBehavior>();
            if (workBehavior != null)
            {
                workBehavior.StartTransporting();
            }

            yield break;
        }
    }

    private GameObject FindRandomTree()
    {
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");

        if (trees.Length == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, trees.Length);
        return trees[randomIndex];
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, string animationName)
    {
        target.Animator.Play(animationName);

        if (pathfinder == null)
        {
            yield break;
        }

        List<Vector3> path = pathfinder.FindPath(target.Transform.position, targetPosition);

        if (path == null || path.Count == 0)
        {
            path = pathfinder.FindPathToArea(target.Transform.position, targetPosition, 2f);
        }

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"No logging path found for {gameObject.name}");
            yield break;
        }

        int pathIndex = 0;

        if (Vector3.Distance(target.Transform.position, path[0]) < 0.1f)
        {
            pathIndex = 1;
        }

        while (pathIndex < path.Count)
        {
            Vector3 currentTarget = path[pathIndex];

            while (Vector3.Distance(target.Transform.position, currentTarget) > 0.1f)
            {
                Vector3 direction = (currentTarget - target.Transform.position).normalized;

                target.Transform.position = Vector3.MoveTowards(
                    target.Transform.position,
                    currentTarget,
                    settings.movementSpeed * Time.deltaTime
                );

                if (settings.enableSpriteFlipping)
                {
                    HandleSpriteFlipping(currentTarget);
                }

                if (target.SpriteRenderer != null)
                {
                    target.SpriteRenderer.sortingOrder = Mathf.RoundToInt(-target.Transform.position.y * 10);
                }

                yield return null;
            }

            pathIndex++;
        }

        // Snap to the actual tree chopping position after reaching nearby walkable area
        target.Transform.position = targetPosition;
    }

    private GameObject FindRandomTreeInRadius()
    {
        GameObject[] trees = GameObject.FindGameObjectsWithTag("Tree");

        if (trees.Length == 0)
        {
            return null;
        }

        List<GameObject> nearbyTrees = new List<GameObject>();

        foreach (GameObject tree in trees)
        {
            float distance = Vector3.Distance(buildingPos, tree.transform.position);

            if (distance <= treeSearchRadius)
            {
                nearbyTrees.Add(tree);
            }
        }

        if (nearbyTrees.Count == 0)
        {
            return FindRandomTree();
        }

        int randomIndex = Random.Range(0, nearbyTrees.Count);
        return nearbyTrees[randomIndex];
    }

    private void HandleSpriteFlipping(Vector3 targetPosition)
    {
        if (target.SpriteRenderer == null) return;

        float direction = targetPosition.x - target.Transform.position.x;

        if (settings.isLeftFacingDefault)
        {
            target.SpriteRenderer.flipX = direction > 0;
        }
        else
        {
            target.SpriteRenderer.flipX = direction < 0;
        }
    }

    private void RepositionToValidPosition()
    {
        if (pathfinder == null)
        {
            return;
        }

        Vector3 currentPos = target.Transform.position;
        Vector3Int currentGridPos = pathfinder.WorldToGridPosition(currentPos);

        if (pathfinder.IsPositionWalkable(currentGridPos))
        {
            return;
        }

        Vector3? validPos = null;

        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) != radius)
                        continue;

                    Vector3Int testPos = currentGridPos + new Vector3Int(x, y, 0);

                    if (pathfinder.IsPositionWalkable(testPos))
                    {
                        validPos = pathfinder.GridToWorldPosition(testPos);
                        break;
                    }
                }

                if (validPos.HasValue)
                    break;
            }

            if (validPos.HasValue)
                break;
        }

        if (validPos.HasValue)
        {
            target.Transform.position = validPos.Value;
        }
    }

    private void OnDestroy()
    {
        StopLogging();
    }
}