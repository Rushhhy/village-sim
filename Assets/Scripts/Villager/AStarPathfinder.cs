using System;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private GridData gridData;
    private PathfindingSettings settings;
    private string debugName;

    public AStarPathfinder(GridData gridData, PathfindingSettings settings, string debugName = "AStarPathfinder")
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
    /// Find a path from start to end using A* algorithm
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
    {
        Vector3Int start = WorldToGridPosition(startWorld);
        Vector3Int end = WorldToGridPosition(endWorld);

        if (settings.enablePathfindingDebug)
            Debug.Log($"{debugName}: Finding path from {start} to {end}");

        // Quick validation
        if (!IsPositionWalkable(start))
        {
            Debug.LogWarning($"{debugName}: Start position {start} not walkable");
            return null;
        }
        if (!IsPositionWalkable(end))
        {
            Debug.LogWarning($"{debugName}: End position {end} not walkable");
            return null;
        }

        // A* algorithm implementation
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector3Int>();
        var allNodes = new Dictionary<Vector3Int, PathNode>();

        // Create start node
        var startNode = new PathNode(start, 0, GetHeuristic(start, end), null);
        openSet.Add(startNode);
        allNodes[start] = startNode;

        int iterations = 0;
        int maxIterations = 1000; // Prevent infinite loops

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;

            // Find node with lowest F cost
            var currentNode = GetLowestFCostNode(openSet);
            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            // Check if we reached the goal
            if (currentNode.Position == end)
            {
                return ReconstructPath(currentNode);
            }

            // Check all neighbors
            foreach (var neighborPos in GetNeighbors(currentNode.Position))
            {
                if (closedSet.Contains(neighborPos) || !IsPositionWalkable(neighborPos))
                    continue;

                float newGCost = currentNode.GCost + GetMovementCost(currentNode.Position, neighborPos);

                PathNode neighborNode;
                if (allNodes.TryGetValue(neighborPos, out neighborNode))
                {
                    // Node already exists, check if this path is better
                    if (newGCost < neighborNode.GCost)
                    {
                        neighborNode.GCost = newGCost;
                        neighborNode.Parent = currentNode;

                        if (!openSet.Contains(neighborNode))
                            openSet.Add(neighborNode);
                    }
                }
                else
                {
                    // Create new node
                    neighborNode = new PathNode(neighborPos, newGCost, GetHeuristic(neighborPos, end), currentNode);
                    allNodes[neighborPos] = neighborNode;
                    openSet.Add(neighborNode);
                }
            }
        }

        Debug.LogWarning($"{debugName}: No path found after {iterations} iterations");
        return null;
    }

    /// <summary>
    /// Find a path to any position within a radius (useful for moving to general areas)
    /// </summary>
    public List<Vector3> FindPathToArea(Vector3 startWorld, Vector3 centerWorld, float radius)
    {
        Vector3Int center = WorldToGridPosition(centerWorld);
        int gridRadius = Mathf.RoundToInt(radius);

        // Try to find path to positions near the center, starting from closest
        for (int r = 0; r <= gridRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) != r && r > 0) continue; // Only check perimeter for r > 0

                    Vector3Int targetPos = center + new Vector3Int(x, y, 0);
                    if (IsPositionWalkable(targetPos))
                    {
                        var path = FindPath(startWorld, GridToWorldPosition(targetPos));
                        if (path != null)
                            return path;
                    }
                }
            }
        }

        return null;
    }

    private PathNode GetLowestFCostNode(List<PathNode> nodes)
    {
        PathNode lowest = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].FCost < lowest.FCost ||
                (nodes[i].FCost == lowest.FCost && nodes[i].HCost < lowest.HCost))
            {
                lowest = nodes[i];
            }
        }
        return lowest;
    }

    private List<Vector3Int> GetNeighbors(Vector3Int position)
    {
        var neighbors = new List<Vector3Int>();

        // 4-directional movement (can be expanded to 8-directional if needed)
        neighbors.Add(position + Vector3Int.right);
        neighbors.Add(position + Vector3Int.left);
        neighbors.Add(position + Vector3Int.up);
        neighbors.Add(position + Vector3Int.down);

        return neighbors;
    }

    private float GetMovementCost(Vector3Int from, Vector3Int to)
    {
        // Basic movement cost - can be enhanced to include terrain costs
        return 1f;
    }

    private float GetHeuristic(Vector3Int from, Vector3Int to)
    {
        // Manhattan distance for 4-directional movement
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
    }

    private List<Vector3> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector3>();
        var currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(GridToWorldPosition(currentNode.Position));
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Checks if a grid position is walkable
    /// </summary>
    public bool IsPositionWalkable(Vector3Int gridPosition)
    {
        return WalkabilityUtility.IsPositionWalkable(gridPosition, gridData, settings);
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

    private bool IsRoadID(int id)
    {
        // Road ID range from your GridData class
        return id >= 0 && id <= 15;
    }
}

/// <summary>
/// Node class for A* pathfinding
/// </summary>
public class PathNode
{
    public Vector3Int Position { get; }
    public float GCost { get; set; } // Distance from start
    public float HCost { get; } // Heuristic distance to end
    public float FCost => GCost + HCost; // Total cost
    public PathNode Parent { get; set; }

    public PathNode(Vector3Int position, float gCost, float hCost, PathNode parent)
    {
        Position = position;
        GCost = gCost;
        HCost = hCost;
        Parent = parent;
    }
}