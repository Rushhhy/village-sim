using System.Collections.Generic;
using UnityEngine;

public static class MovementService
{
    public static HashSet<Tile> ComputeReachable(Unit unit, GridManager grid)
    {
        var result = new HashSet<Tile>();
        var start = unit.CurrentTile;
        if (start == null) return result;

        var q = new Queue<(Tile tile, int dist)>();
        q.Enqueue((start, 0));
        result.Add(start);

        while (q.Count > 0)
        {
            var (t, dist) = q.Dequeue();
            if (dist == unit.moveRange) continue;

            foreach (var n in grid.GetNeighbors(t))
            {
                if (n == null) continue;
                if (!n.Walkable) continue;

                // Block occupied tiles (except start)
                if (n.Occupant != null && n != start) continue;

                if (result.Contains(n)) continue;

                result.Add(n);
                q.Enqueue((n, dist + 1));
            }
        }

        return result;
    }

    public static Tile ChooseBestTileTowards(HashSet<Tile> reachable, Tile current, Tile target)
    {
        if (current == null) return null;
        if (target == null) return current;

        Tile best = current;
        int bestDist = Manhattan(current.Coord, target.Coord);

        foreach (var t in reachable)
        {
            if (t == null) continue;
            if (t.Occupant != null && t != current) continue;

            int d = Manhattan(t.Coord, target.Coord);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    public static bool IsInRange(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return false;
        if (attacker.CurrentTile == null || target.CurrentTile == null) return false;

        int d = Manhattan(attacker.CurrentTile.Coord, target.CurrentTile.Coord);
        return d <= attacker.attackRange;
    }

    public static int Manhattan(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    /// <summary>
    /// BFS path from unit's current tile to target tile.
    /// Returns a list of tiles to walk through, NOT including the start tile,
    /// so the first element is the first step.
    /// </summary>
    public static List<Tile> ComputePath(Unit unit, GridManager grid, Tile target)
    {
        var path = new List<Tile>();

        if (unit == null || grid == null || target == null)
            return path;

        Tile start = unit.CurrentTile;
        if (start == null)
            return path;

        var q = new Queue<Tile>();
        var visited = new HashSet<Tile>();
        var cameFrom = new Dictionary<Tile, Tile>();

        visited.Add(start);
        q.Enqueue(start);

        bool found = false;

        while (q.Count > 0)
        {
            Tile current = q.Dequeue();
            if (current == target)
            {
                found = true;
                break;
            }

            foreach (var n in grid.GetNeighbors(current))
            {
                if (n == null) continue;
                if (visited.Contains(n)) continue;
                if (!n.Walkable) continue;

                // Block occupied tiles except:
                // - the starting tile
                // - the target tile (in case you ever allow moving into it)
                if (n.Occupant != null && n != start && n != target) continue;

                visited.Add(n);
                cameFrom[n] = current;
                q.Enqueue(n);
            }
        }

        if (!found)
        {
            // No path found – return empty list
            return path;
        }

        // Reconstruct path from target back to start
        Tile step = target;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Reverse(); // now first is the first step
        return path;
    }
    public static Tile ChooseBestRangedTile(
    HashSet<Tile> reachable,
    Tile current,
    Tile target,
    int attackRange
)
    {
        if (current == null) return null;
        if (target == null) return current;
        if (reachable == null || reachable.Count == 0) return current;

        Tile bestInRange = null;
        int bestDistInRange = -1; // we want the *largest* distance <= attackRange

        foreach (var t in reachable)
        {
            if (t == null) continue;
            if (t.Occupant != null && t != current) continue;

            int d = Manhattan(t.Coord, target.Coord);

            // Tile is in attack range
            if (d <= attackRange)
            {
                // Prefer the tile that keeps us *farthest* from the target
                if (d > bestDistInRange)
                {
                    bestDistInRange = d;
                    bestInRange = t;
                }
            }
        }

        // If we found at least one tile in-range, use it
        if (bestInRange != null)
            return bestInRange;

        // Otherwise, fall back to "get as close as possible"
        return ChooseBestTileTowards(reachable, current, target);
    }
}
