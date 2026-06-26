using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class PathfindingSettings
{

    [Header("Legacy Settings")]
    public int maxPathfindingAttempts = 10;
    public bool allowWalkingOnRoads = true;
    [Tooltip("Allow walking through nature elements like trees and rocks")]
    public bool allowWalkingThroughNature = true;
    [Tooltip("Enable detailed pathfinding logs for debugging")]
    public bool enablePathfindingDebug = false;
    [Tooltip("Maximum radius to search for alternative walkable positions")]
    public int maxRepositionSearchRadius = 10;
    [Tooltip("Number of grid cells to validate ahead during movement")]
    public int movementLookaheadCells = 3;

    // Static method to get the predefined walkable corridor positions
    public static HashSet<Vector3Int> GetWalkableCorridorPositions()
    {
        var corridorPositions = new HashSet<Vector3Int>();

        // Row at y=31: from (-5, 31, 0) to (9, 31, 0)
        for (int x = -5; x <= 9; x++)
        {
            corridorPositions.Add(new Vector3Int(x, 31, 0));
        }

        // Row at y=28: from (-4, 28, 0) to (11, 28, 0)
        for (int x = -4; x <= 11; x++)
        {
            corridorPositions.Add(new Vector3Int(x, 28, 0));
        }

        // Individual positions
        corridorPositions.Add(new Vector3Int(1, 29, 0));
        corridorPositions.Add(new Vector3Int(1, 30, 0));
        corridorPositions.Add(new Vector3Int(11, 29, 0));
        corridorPositions.Add(new Vector3Int(8, 29, 0));
        corridorPositions.Add(new Vector3Int(8, 30, 0));

        corridorPositions.Remove(new Vector3Int(3, 31, 0));
        corridorPositions.Remove(new Vector3Int(4, 31, 0));
        corridorPositions.Remove(new Vector3Int(5, 31, 0));
        corridorPositions.Remove(new Vector3Int(6, 31, 0));

        return corridorPositions;
    }
}
public class GridPathfinder
{
    private GridData gridData;
    private PathfindingSettings settings;
    private string debugName; // For debug logging

    public GridPathfinder(GridData gridData, PathfindingSettings settings, string debugName = "GridPathfinder")
    {
        this.gridData = gridData;
        this.settings = settings;
        this.debugName = debugName;
    }

    public void UpdateSettings(PathfindingSettings newSettings)
    {
        this.settings = newSettings;
    }

    public void UpdateGridData(GridData newGridData)
    {
        this.gridData = newGridData;
    }

    /// <summary>
    /// Finds a random walkable position within the specified radius from the center point
    /// </summary>
    public Vector3? FindRandomWalkablePosition(Vector3 centerWorldPos, float radius)
    {
        Vector3Int centerGridPos = WorldToGridPosition(centerWorldPos);

        for (int attempt = 0; attempt < settings.maxPathfindingAttempts; attempt++)
        {
            // Choose random point within radius
            Vector2 randomDirection = Random.insideUnitCircle * radius;
            Vector3 potentialTarget = centerWorldPos + new Vector3(randomDirection.x, randomDirection.y, 0);
            Vector3Int targetGridPos = WorldToGridPosition(potentialTarget);

            // Check if target position is walkable
            if (IsPositionWalkable(targetGridPos))
            {
                // Check if there's a clear path to get there
                if (HasClearPath(centerGridPos, targetGridPos))
                {
                    if (settings.enablePathfindingDebug)
                        Debug.Log($"{debugName}: Found valid random position at {targetGridPos}");
                    return potentialTarget;
                }
            }
        }

        if (settings.enablePathfindingDebug)
            Debug.Log($"{debugName}: No valid random position found within radius {radius}");
        return null;
    }

    /// <summary>
    /// Checks if there's a clear path between two world positions
    /// </summary>
    public bool HasClearPath(Vector3 startWorldPos, Vector3 endWorldPos)
    {
        Vector3Int start = WorldToGridPosition(startWorldPos);
        Vector3Int end = WorldToGridPosition(endWorldPos);
        return HasClearPath(start, end);
    }

    /// <summary>
    /// Checks if there's a clear path between two grid positions
    /// </summary>
    public bool HasClearPath(Vector3Int start, Vector3Int end)
    {
        // Get all positions that need to be checked along the path
        HashSet<Vector3Int> pathCells = GetAllPathCells(start, end);

        // Check every cell along the path
        foreach (Vector3Int cell in pathCells)
        {
            if (!IsPositionWalkable(cell))
            {
                if (settings.enablePathfindingDebug)
                    Debug.Log($"{debugName}: Path blocked at {cell}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Validates if a position will remain walkable during movement with lookahead
    /// </summary>
    public bool ValidateMovementPath(Vector3 startPos, Vector3 targetPos, float progress)
    {
        // Calculate current intended position
        Vector3 currentIntendedPos = Vector3.Lerp(startPos, targetPos, progress);
        Vector3Int currentGridPos = WorldToGridPosition(currentIntendedPos);

        // Validate current position
        if (!IsPositionWalkable(currentGridPos))
        {
            if (settings.enablePathfindingDebug)
                Debug.Log($"{debugName}: Movement blocked at {currentGridPos}");
            return false;
        }

        // Additional validation: check positions ahead in the path
        float lookaheadProgress = Mathf.Min(1f, progress + (settings.movementLookaheadCells * 0.1f));
        Vector3 lookaheadPos = Vector3.Lerp(startPos, targetPos, lookaheadProgress);
        Vector3Int lookaheadGridPos = WorldToGridPosition(lookaheadPos);

        if (!IsPositionWalkable(lookaheadGridPos))
        {
            if (settings.enablePathfindingDebug)
                Debug.Log($"{debugName}: Lookahead detected obstacle at {lookaheadGridPos}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the nearest walkable position to a blocked position
    /// </summary>
    public Vector3? FindNearestWalkablePosition(Vector3 blockedWorldPos)
    {
        Vector3Int blockedGridPos = WorldToGridPosition(blockedWorldPos);
        Vector3Int? walkableGridPos = FindNearestWalkablePosition(blockedGridPos);

        if (walkableGridPos.HasValue)
        {
            return GridToWorldPosition(walkableGridPos.Value);
        }

        return null;
    }

    /// <summary>
    /// Finds the nearest walkable grid position to a blocked grid position
    /// </summary>
    public Vector3Int? FindNearestWalkablePosition(Vector3Int blockedPosition)
    {
        // Spiral search outward from the blocked position
        for (int radius = 1; radius <= settings.maxRepositionSearchRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Only check positions on the edge of the current radius
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;

                    Vector3Int checkPos = blockedPosition + new Vector3Int(x, y, 0);
                    if (IsPositionWalkable(checkPos))
                    {
                        return checkPos;
                    }
                }
            }
        }

        if (settings.enablePathfindingDebug)
            Debug.Log($"{debugName}: No walkable position found within {settings.maxRepositionSearchRadius} cells of {blockedPosition}");
        return null;
    }

    /// <summary>
    /// Checks if a world position is walkable
    /// </summary>
    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        return IsPositionWalkable(WorldToGridPosition(worldPosition));
    }

    /// <summary>
    /// Checks if a grid position is walkable
    /// </summary>
    public bool IsPositionWalkable(Vector3Int gridPosition)
    {
        bool walkable = WalkabilityUtility.IsPositionWalkable(gridPosition, gridData, settings);

        if (!walkable && settings != null && settings.enablePathfindingDebug)
        {
            Debug.Log($"{debugName}: Position {gridPosition} is not walkable");
        }

        return walkable;
    }

    /// <summary>
    /// Pre-validates an entire path before starting movement
    /// </summary>
    public bool PreValidatePath(Vector3 startPos, Vector3 targetPos)
    {
        Vector3Int startGridPos = WorldToGridPosition(startPos);
        Vector3Int targetGridPos = WorldToGridPosition(targetPos);

        HashSet<Vector3Int> pathCells = GetAllPathCells(startGridPos, targetGridPos);
        foreach (Vector3Int cell in pathCells)
        {
            if (!IsPositionWalkable(cell))
            {
                if (settings.enablePathfindingDebug)
                    Debug.Log($"{debugName}: Pre-validation failed - path blocked at {cell}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Converts world position to grid position
    /// </summary>
    public Vector3Int WorldToGridPosition(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y),
            0
        );
    }

    /// <summary>
    /// Converts grid position to world position (center of cell)
    /// </summary>
    public Vector3 GridToWorldPosition(Vector3Int gridPosition)
    {
        return new Vector3(
            gridPosition.x + 0.5f, // Center of grid cell
            gridPosition.y + 0.5f,
            0
        );
    }

    // Get all grid cells that could potentially be intersected by the movement path
    private HashSet<Vector3Int> GetAllPathCells(Vector3Int start, Vector3Int end)
    {
        HashSet<Vector3Int> cells = new HashSet<Vector3Int>();

        // Add start and end positions
        cells.Add(start);
        cells.Add(end);

        // Calculate the bounding box of the path
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        // Check all cells in the bounding rectangle
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                // Check if this cell intersects with the movement line
                if (DoesCellIntersectPath(cell, start, end))
                {
                    cells.Add(cell);
                }
            }
        }

        return cells;
    }

    // Check if a grid cell intersects with the movement path
    private bool DoesCellIntersectPath(Vector3Int cell, Vector3Int start, Vector3Int end)
    {
        // Convert grid positions to world positions (cell centers)
        Vector2 startWorld = new Vector2(start.x + 0.5f, start.y + 0.5f);
        Vector2 endWorld = new Vector2(end.x + 0.5f, end.y + 0.5f);

        // Cell bounds
        Vector2 cellMin = new Vector2(cell.x, cell.y);
        Vector2 cellMax = new Vector2(cell.x + 1f, cell.y + 1f);

        // Use line-rectangle intersection test
        return LineIntersectsRect(startWorld, endWorld, cellMin, cellMax);
    }

    // Line-rectangle intersection test
    private bool LineIntersectsRect(Vector2 lineStart, Vector2 lineEnd, Vector2 rectMin, Vector2 rectMax)
    {
        // If either endpoint is inside the rectangle, intersection exists
        if (IsPointInRect(lineStart, rectMin, rectMax) || IsPointInRect(lineEnd, rectMin, rectMax))
            return true;

        // Check if line intersects any of the four rectangle edges
        Vector2 topLeft = new Vector2(rectMin.x, rectMax.y);
        Vector2 topRight = rectMax;
        Vector2 bottomLeft = rectMin;
        Vector2 bottomRight = new Vector2(rectMax.x, rectMin.y);

        return LineIntersectsLine(lineStart, lineEnd, topLeft, topRight) ||
               LineIntersectsLine(lineStart, lineEnd, topRight, bottomRight) ||
               LineIntersectsLine(lineStart, lineEnd, bottomRight, bottomLeft) ||
               LineIntersectsLine(lineStart, lineEnd, bottomLeft, topLeft);
    }

    private bool IsPointInRect(Vector2 point, Vector2 rectMin, Vector2 rectMax)
    {
        return point.x >= rectMin.x && point.x <= rectMax.x &&
               point.y >= rectMin.y && point.y <= rectMax.y;
    }

    private bool LineIntersectsLine(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End)
    {
        float denominator = (line1Start.x - line1End.x) * (line2Start.y - line2End.y) -
                           (line1Start.y - line1End.y) * (line2Start.x - line2End.x);

        if (Mathf.Abs(denominator) < 0.0001f) return false; // Lines are parallel

        float t = ((line1Start.x - line2Start.x) * (line2Start.y - line2End.y) -
                  (line1Start.y - line2Start.y) * (line2Start.x - line2End.x)) / denominator;
        float u = -((line1Start.x - line1End.x) * (line1Start.y - line2Start.y) -
                   (line1Start.y - line1End.y) * (line1Start.x - line2Start.x)) / denominator;

        return t >= 0 && t <= 1 && u >= 0 && u <= 1;
    }
}