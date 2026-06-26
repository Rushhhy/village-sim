using System.Collections;
using UnityEngine;

[System.Serializable]
public class IdleBehaviorSettings
{
    [Header("Movement Settings")]
    public float movementRadius = 2f;
    public float movementSpeed = 1f;

    [Header("Timing Settings")]
    public float waitTimeMin = 2f;
    public float waitTimeMax = 5f;
    public float moveTimeMin = 3f;
    public float moveTimeMax = 5f;

    [Header("Animation Settings")]
    public string idleAnimationName = "Idle";
    public string walkAnimationName = "Walk";

    [Header("Sprite Flipping")]
    public bool enableSpriteFlipping = true;
    public bool isLeftFacingDefault = true;

    [Header("Pathfinding")]
    public PathfindingSettings pathfindingSettings;
}

public interface IIdleBehaviorTarget
{
    Transform Transform { get; }
    Animator Animator { get; }
    SpriteRenderer SpriteRenderer { get; }
    bool ShouldContinueIdling { get; }
}

public class IdleBehavior : MonoBehaviour
{
    [SerializeField] private IdleBehaviorSettings settings;
    [SerializeField] private GridData gridData; // Reference to your grid system

    private IIdleBehaviorTarget target;
    private GridPathfinder pathfinder;
    private Vector3 idleStartPosition;
    private Vector3 idleTargetPosition;
    private Coroutine idleCoroutine;
    private bool isIdling = false;

    public bool IsIdling => isIdling;
    public Vector3 IdleStartPosition
    {
        get => idleStartPosition;
        set => idleStartPosition = value;
    }

    private void Awake()
    {
        target = GetComponent<IIdleBehaviorTarget>();
        if (target == null)
        {
            Debug.LogError($"IdleBehavior requires a component that implements IIdleBehaviorTarget on {gameObject.name}");
        }

        // Find GridData if not assigned
        if (gridData == null)
        {
            gridData = GameObject.Find("PlacementSystem").GetComponent<PlacementSystem>().gridData;
            if (gridData == null)
            {
                Debug.LogError($"GridData not found for IdleBehavior on {gameObject.name}");
            }
        }

        // Initialize pathfinder
        if (gridData != null)
        {
            pathfinder = new GridPathfinder(gridData, settings.pathfindingSettings, gameObject.name);
        }
    }

    public void StartIdling()
    {
        if (isIdling || target == null || pathfinder == null) return;

        ResetSpriteOrientation();

        // Validate starting position and reposition if necessary
        if (!pathfinder.IsPositionWalkable(idleStartPosition))
        {
            Vector3? newPos = pathfinder.FindNearestWalkablePosition(idleStartPosition);
            if (newPos.HasValue)
            {
                idleStartPosition = newPos.Value;
            }
            else
            {
                Debug.LogWarning($"Cannot start idling for {gameObject.name} - no walkable position found");
                return;
            }
        }

        isIdling = true;
        target.Transform.position = idleStartPosition;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }

        idleCoroutine = StartCoroutine(IdleBehaviorCoroutine());
    }

    public void StopIdling()
    {
        if (!isIdling) return;

        isIdling = false;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
            idleCoroutine = null;
        }

        // Play idle animation when stopping
        if (target?.Animator != null)
        {
            target.Animator.Play(settings.idleAnimationName);
        }
    }

    public void SetIdleStartPosition(Vector3 position)
    {
        idleStartPosition = position;
    }

    private IEnumerator IdleBehaviorCoroutine()
    {
        // Start with idle animation
        if (target.Animator != null)
        {
            target.Animator.Play(settings.idleAnimationName);
        }

        while (isIdling && target.ShouldContinueIdling)
        {
            // Wait for random time
            float waitTime = Random.Range(settings.waitTimeMin, settings.waitTimeMax);
            yield return new WaitForSeconds(waitTime);

            if (!isIdling || !target.ShouldContinueIdling) break;

            // Try to find a valid target position
            Vector3? targetPosition = pathfinder.FindRandomWalkablePosition(idleStartPosition, settings.movementRadius);
            if (targetPosition.HasValue)
            {
                idleTargetPosition = targetPosition.Value;

                // Move to target
                float moveTime = Random.Range(settings.moveTimeMin, settings.moveTimeMax);
                yield return StartCoroutine(MoveToPositionWithFlip(idleTargetPosition, moveTime));

                if (!isIdling || !target.ShouldContinueIdling) break;
            }
            else
            {
                // If no valid position found, wait a bit longer before trying again
                yield return new WaitForSeconds(1f);
            }
        }

        isIdling = false;
    }

    private IEnumerator MoveToPositionWithFlip(Vector3 targetPos, float duration)
    {
        Vector3 startPos = target.Transform.position;
        float elapsed = 0f;

        // Play walk animation
        if (target.Animator != null)
        {
            target.Animator.Play(settings.walkAnimationName);
        }

        // Pre-validate the entire path before starting movement
        if (!pathfinder.PreValidatePath(startPos, targetPos))
        {
            yield break; // Exit immediately if path is blocked
        }

        Vector3 lastValidPosition = startPos;

        while (elapsed < duration && isIdling && target.ShouldContinueIdling)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            // Validate movement path with lookahead
            if (!pathfinder.ValidateMovementPath(startPos, targetPos, progress))
            {
                break; // Stop movement if path becomes blocked
            }

            // Calculate and move to current position
            Vector3 currentIntendedPos = Vector3.Lerp(startPos, targetPos, progress);
            target.Transform.position = currentIntendedPos;
            lastValidPosition = currentIntendedPos;

            // Handle sprite flipping
            if (settings.enableSpriteFlipping)
            {
                HandleSpriteFlipping(targetPos - startPos);
            }

            // Update sorting order
            if (target.SpriteRenderer != null)
            {
                target.SpriteRenderer.sortingOrder = (int)(-target.Transform.position.y * 10);
            }

            yield return null;
        }

        // Ensure final position is walkable before setting it
        if (isIdling && target.ShouldContinueIdling && pathfinder.IsPositionWalkable(targetPos))
        {
            target.Transform.position = targetPos;
        }
        else
        {
            // Stay at last valid position
            target.Transform.position = lastValidPosition;
        }

        // Return to idle animation
        if (target.Animator != null)
        {
            target.Animator.Play(settings.idleAnimationName);
        }
    }

    private void HandleSpriteFlipping(Vector3 direction)
    {
        if (Mathf.Abs(direction.x) < 0.1f) return; // No significant horizontal movement

        bool shouldFlip = settings.isLeftFacingDefault ? direction.x > 0 : direction.x < 0;

        float scaleX = shouldFlip ? -Mathf.Abs(target.Transform.localScale.x) : Mathf.Abs(target.Transform.localScale.x);
        target.Transform.localScale = new Vector3(scaleX, target.Transform.localScale.y, target.Transform.localScale.z);
    }

    private void ResetSpriteOrientation()
    {
        if (target?.SpriteRenderer != null)
        {
            target.SpriteRenderer.flipX = false; // Reset flipX from WorkBehavior
        }

        if (target?.Transform != null)
        {
            // Reset localScale to positive (reset any scale-based flipping)
            Vector3 scale = target.Transform.localScale;
            target.Transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        }
    }
}