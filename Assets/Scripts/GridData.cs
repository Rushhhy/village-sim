using System;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    private readonly Dictionary<Vector3Int, PlacementData> placedObjects = new();
    private readonly Dictionary<Vector3Int, PlacementData> placedDecorations = new();

    private const int MIN_ROAD_ID = 0;
    private const int MAX_ROAD_ID = 15;

    public HashSet<Vector3Int> mapBoundaries = new HashSet<Vector3Int>()
    {
        new Vector3Int(1, 29, 0), new Vector3Int(0, 31, 0), new Vector3Int(1, 31, 0), new Vector3Int(2, 31, 0), new Vector3Int(-5, 31, 0), new Vector3Int(-5, 30, 0), new Vector3Int(-5, 29, 0), new Vector3Int(7, 31, 0),
        new Vector3Int(8, 31, 0), new Vector3Int(19, 34, 0), new Vector3Int(19, 35, 0), new Vector3Int(22, 40, 0), new Vector3Int(22, 41, 0), new Vector3Int(22, 42, 0), new Vector3Int(19, 51, 0), new Vector3Int(19, 52, 0), new Vector3Int(8, 56, 0), new Vector3Int(9, 56, 0),
        new Vector3Int(10, 56, 0), new Vector3Int(11, 56, 0), new Vector3Int(12, 56, 0), new Vector3Int(1, 56, 0), new Vector3Int(2, 56, 0), new Vector3Int(3, 56, 0), new Vector3Int(-11, 56, 0), new Vector3Int(-10, 56, 0), new Vector3Int(-9, 56, 0), new Vector3Int(-20, 56, 0),
        new Vector3Int(-19, 56, 0), new Vector3Int(-18, 56, 0), new Vector3Int(-17, 56, 0), new Vector3Int(-16, 56, 0), new Vector3Int(-23, 54, 0), new Vector3Int(-27, 53, 0), new Vector3Int(-26, 53, 0), new Vector3Int(-29, 53, 0), new Vector3Int(-29, 52, 0), new Vector3Int(-31, 30, 0),
        new Vector3Int(-31, 29, 0), new Vector3Int(-31, 28, 0), new Vector3Int(-31, 27, 0), new Vector3Int(-31, 26, 0), new Vector3Int(-32, 28, 0), new Vector3Int(-32, 29, 0), new Vector3Int(-24, 24, 0), new Vector3Int(-23, 24, 0), new Vector3Int(-22, 24, 0), new Vector3Int(-21, 24, 0),
        new Vector3Int(-20, 24, 0), new Vector3Int(-19, 24, 0), new Vector3Int(-18, 24, 0), new Vector3Int(-28, 36, 0), new Vector3Int(-23, 53, 0), new Vector3Int(13, 33, 0), new Vector3Int(8, 28, 0), new Vector3Int(9, 28, 0), new Vector3Int(10, 28, 0), new Vector3Int(11, 28, 0),
        new Vector3Int(9, 31, 0), new Vector3Int(0, 30, 0), new Vector3Int(2, 30, 0), new Vector3Int(7, 30, 0), new Vector3Int(9, 30, 0)
    };

    public HashSet<Vector3Int> positionsToRemove = new HashSet<Vector3Int>()
    {
        new Vector3Int(-5, 29, 0), new Vector3Int(-24, 32, 0), new Vector3Int(-23, 32, 0), new Vector3Int(-19, 32, 0), new Vector3Int(-18, 32, 0), new Vector3Int(12, 35, 0), new Vector3Int(13, 35, 0),
        new Vector3Int(17, 35, 0), new Vector3Int(18, 35, 0), new Vector3Int(-24, 53, 0), new Vector3Int(-23, 54, 0), new Vector3Int(-22, 54, 0), new Vector3Int(-21, 54, 0), new Vector3Int(-20, 54, 0),
        new Vector3Int(-20, 53, 0), new Vector3Int(-18, 56, 0), new Vector3Int(-19, 55, 0), new Vector3Int(-19, 54, 0), new Vector3Int(-20, 55, 0), new Vector3Int(-20, 56, 0), new Vector3Int(-19, 56, 0),
        new Vector3Int(-20, 57, 0), new Vector3Int(-21, 56, 0), new Vector3Int(-22, 56, 0), new Vector3Int(-23, 56, 0), new Vector3Int(-24, 55, 0), new Vector3Int(-24, 56, 0), new Vector3Int(9, 56, 0),
        new Vector3Int(9, 55, 0), new Vector3Int(9, 54, 0), new Vector3Int(10, 56, 0), new Vector3Int(10, 55, 0), new Vector3Int(10, 54, 0), new Vector3Int(10, 53, 0), new Vector3Int(11, 56, 0),
        new Vector3Int(11, 55, 0), new Vector3Int(12, 56, 0), new Vector3Int(12, 55, 0), new Vector3Int(13, 56, 0), new Vector3Int(13, 55, 0), new Vector3Int(14, 56, 0), new Vector3Int(14, 55, 0),
        new Vector3Int(14, 54, 0), new Vector3Int(14, 53, 0), new Vector3Int(11, 53, 0), new Vector3Int(12, 54, 0), new Vector3Int(11, 54, 0), new Vector3Int(8, 56, 0), new Vector3Int(14, 35, 0),
        new Vector3Int(-23, 53, 0), new Vector3Int(-21, 53, 0), new Vector3Int(-22, 32, 0), new Vector3Int(-20, 32, 0), new Vector3Int(16, 35, 0), new Vector3Int(-8, 54, 0), new Vector3Int(-7, 54, 0),
        new Vector3Int(-6, 54, 0), new Vector3Int(-4, 54, 0), new Vector3Int(-3, 54, 0), new Vector3Int(-2, 54, 0), new Vector3Int(10, 32, 0), new Vector3Int(11, 32, 0), new Vector3Int(9, 29, 0), new Vector3Int(7, 29, 0)
    };

    public HashSet<Vector3Int> positionHasNature = new();

    private static readonly Vector3Int[] NeighborDirections =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down
    };

    public bool[] GetNeighbouringRoads(Vector3Int position)
    {
        var roads = new bool[4];

        for (int i = 0; i < NeighborDirections.Length; i++)
        {
            int id = GetIDAtPosition(position + NeighborDirections[i]);
            roads[i] = IsRoadID(id);
        }

        return roads;
    }

    public List<Vector3Int> GetNeighbourRoadPositions(Vector3Int position)
    {
        var result = new List<Vector3Int>(4);

        for (int i = 0; i < NeighborDirections.Length; i++)
        {
            Vector3Int neighbor = position + NeighborDirections[i];

            if (IsRoadID(GetIDAtPosition(neighbor)))
            {
                result.Add(neighbor);
            }
        }

        return result;
    }

    private static bool IsRoadID(int id) => id >= MIN_ROAD_ID && id <= MAX_ROAD_ID;

    public int GetIDAtPosition(Vector3Int position)
    {
        if (placedDecorations.TryGetValue(position, out var decor))
            return decor.ID;

        if (placedObjects.TryGetValue(position, out var obj))
            return obj.ID;

        return -1;
    }

    public int GetIndexAtPosition(Vector3Int position)
    {
        if (placedObjects.TryGetValue(position, out var obj))
            return obj.PlaceObjectIndex;

        if (placedDecorations.TryGetValue(position, out var decor))
            return decor.PlaceObjectIndex;

        return -1;
    }

    public PlacementData GetPlacementDataAt(Vector3Int position)
    {
        if (placedObjects.TryGetValue(position, out var obj))
            return obj;

        if (placedDecorations.TryGetValue(position, out var decor))
            return decor;

        return null;
    }

    public bool HasAnyObject(Vector3Int position)
    {
        return placedObjects.ContainsKey(position) || placedDecorations.ContainsKey(position);
    }

    public void AddObjectAt(Vector3Int gridPosition, Vector2Int size, int id, int index, ObjectType type)
    {
        var positions = CalculatePositions(gridPosition, size);
        var data = new PlacementData(positions, id, index, type);

        var dict = GetDictionary(type);

        foreach (var pos in positions)
        {
            if (dict.ContainsKey(pos))
                throw new InvalidOperationException($"Position {pos} is already occupied.");
        }

        foreach (var pos in positions)
        {
            dict[pos] = data;
        }
    }

    public void AddFixedObjects(IReadOnlyList<Vector3Int> positions, int id, int index)
    {
        var data = new PlacementData(new List<Vector3Int>(positions), id, index, ObjectType.Object);

        foreach (var pos in positions)
        {
            if (placedObjects.ContainsKey(pos))
                throw new InvalidOperationException($"Position {pos} is already occupied.");
        }

        foreach (var pos in positions)
        {
            placedObjects[pos] = data;
        }
    }

    public bool RemoveObjectAt(Vector3Int position)
    {
        if (TryRemoveFromDict(position, placedObjects))
            return true;

        if (TryRemoveFromDict(position, placedDecorations))
            return true;

        return false;
    }

    private bool TryRemoveFromDict(Vector3Int position, Dictionary<Vector3Int, PlacementData> dict)
    {
        if (!dict.TryGetValue(position, out var data))
            return false;

        foreach (var pos in data.occupiedPositions)
            dict.Remove(pos);

        return true;
    }

    public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int size)
    {
        var positions = CalculatePositions(gridPosition, size);

        foreach (var pos in positions)
        {
            if (!mapBoundaries.Contains(pos)) return false;
            if (positionHasNature.Contains(pos)) return false;
            if (HasAnyObject(pos)) return false;
        }

        return true;
    }

    public void AddPositionsInArea(Vector3Int bottomLeft, Vector3Int topRight)
    {
        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                mapBoundaries.Add(new Vector3Int(x, y, bottomLeft.z));
            }
        }
    }

    public void RemovePositionsInArea(Vector3Int bottomLeft, Vector3Int topRight)
    {
        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                mapBoundaries.Remove(new Vector3Int(x, y, bottomLeft.z));
            }
        }
    }

    private Dictionary<Vector3Int, PlacementData> GetDictionary(ObjectType type)
    {
        return type == ObjectType.Object ? placedObjects : placedDecorations;
    }

    private static List<Vector3Int> CalculatePositions(Vector3Int origin, Vector2Int size)
    {
        var result = new List<Vector3Int>(size.x * size.y);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                result.Add(origin + new Vector3Int(x, y, 0));
            }
        }

        return result;
    }
}

public enum ObjectType
{
    Object = 0,
    Decoration = 1
}

public class PlacementData
{
    public List<Vector3Int> occupiedPositions { get; }
    public int ID { get; }
    public int PlaceObjectIndex { get; set; }
    public ObjectType Type { get; }

    public PlacementData(List<Vector3Int> occupiedPositions, int id, int index, ObjectType type)
    {
        this.occupiedPositions = occupiedPositions ?? throw new ArgumentNullException(nameof(occupiedPositions));
        ID = id;
        PlaceObjectIndex = index;
        Type = type;
    }
}