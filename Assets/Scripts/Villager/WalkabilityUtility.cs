using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared utility for consistent walkability checks across all systems
/// </summary>
public static class WalkabilityUtility
{
    // Positions that are never walkable regardless of any other conditions
    private static readonly HashSet<Vector3Int> ForbiddenPositions = new HashSet<Vector3Int>
    {
        new Vector3Int(0, 30, 0),
        new Vector3Int(2, 30, 0),
        new Vector3Int(7, 30, 0),
        new Vector3Int(9, 30, 0),
        new Vector3Int(12, 33, 0)
    };

    /// <summary>
    /// Universal walkability check used by all pathfinding and validation systems
    /// </summary>
    /// <param name="gridPosition">Position to check</param>
    /// <param name="gridData">Grid data for boundary and object checks</param>
    /// <param name="settings">Pathfinding settings for behavior flags</param>
    /// <returns>True if position is walkable</returns>
    public static bool IsPositionWalkable(Vector3Int gridPosition, GridData gridData, PathfindingSettings settings)
    {
        if (gridData == null)
        {
            Debug.LogWarning("WalkabilityUtility: GridData is null, assuming position is walkable");
            return true;
        }

        // First check: Positions that are never walkable
        if (ForbiddenPositions.Contains(gridPosition))
        {
            return false;
        }

        // Special case: Walkable corridors are always walkable (even outside map boundaries)
        var walkableCorridors = PathfindingSettings.GetWalkableCorridorPositions();
        if (walkableCorridors.Contains(gridPosition))
        {
            return true;
        }

        // For non-corridor positions, check if position is within map boundaries
        if (!gridData.mapBoundaries.Contains(gridPosition))
        {
            return false;
        }

        // Check if position has nature (trees, rocks, etc.)
        if (gridData.positionHasNature.Contains(gridPosition))
        {
            return settings?.allowWalkingThroughNature ?? false;
        }

        // Check for placed objects
        int id = gridData.GetIDAtPosition(gridPosition);

        // If no object at position, it's walkable
        if (id == -1)
        {
            return true;
        }

        // If roads are walkable and this is a road, allow it
        if (settings?.allowWalkingOnRoads == true && IsRoadID(id))
        {
            return true;
        }

        // Otherwise, position is blocked
        return false;
    }

    /// <summary>
    /// Overload for systems that don't need pathfinding settings (uses safe defaults)
    /// </summary>
    public static bool IsPositionWalkable(Vector3Int gridPosition, GridData gridData)
    {
        // Create default settings for validation systems
        var defaultSettings = new PathfindingSettings();
        return IsPositionWalkable(gridPosition, gridData, defaultSettings);
    }

    /// <summary>
    /// Add a position to the forbidden list at runtime (if needed)
    /// </summary>
    public static void AddForbiddenPosition(Vector3Int position)
    {
        ForbiddenPositions.Add(position);
    }

    /// <summary>
    /// Remove a position from the forbidden list at runtime (if needed)
    /// </summary>
    public static void RemoveForbiddenPosition(Vector3Int position)
    {
        ForbiddenPositions.Remove(position);
    }

    /// <summary>
    /// Check if a position is in the forbidden list
    /// </summary>
    public static bool IsForbiddenPosition(Vector3Int position)
    {
        return ForbiddenPositions.Contains(position);
    }

    private static bool IsRoadID(int id)
    {
        // Road ID range
        return id >= 0 && id <= 15;
    }
}