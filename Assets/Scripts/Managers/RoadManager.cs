using System;
using System.Collections.Generic;
using UnityEngine;

public class RoadManager : MonoBehaviour
{
    [SerializeField] private PlacementSystem placementSystem;

    // Road types
    private const int VERTICAL_ROAD = 0;
    private const int HORIZONTAL_ROAD_LEFT = 1;
    private const int HORIZONTAL_ROAD_RIGHT = 2;

    private const int DEAD_END_UP = 3;
    private const int DEAD_END_DOWN = 4;
    private const int DEAD_END_LEFT = 5;
    private const int DEAD_END_RIGHT = 6;

    private const int CORNER_TOP_RIGHT = 7;
    private const int CORNER_BOTTOM_RIGHT = 8;
    private const int CORNER_BOTTOM_LEFT = 9;
    private const int CORNER_TOP_LEFT = 10;

    private const int THREE_WAY_TOP_RIGHT_LEFT = 11;
    private const int THREE_WAY_BOTTOM_RIGHT_LEFT = 12;
    private const int THREE_WAY_TOP_RIGHT_BOTTOM = 13;
    private const int THREE_WAY_TOP_LEFT_BOTTOM = 14;

    private const int FOUR_WAY_INTERSECTION = 15;

    private const int RIGHT = 0;
    private const int LEFT = 1;
    private const int TOP = 2;
    private const int BOTTOM = 3;

    public void FixRoadAt(Vector3Int position, bool[] neighbours)
    {
        int count = CountConnectedRoads(neighbours);

        switch (count)
        {
            case 0:
                Place(position, VERTICAL_ROAD);
                break;

            case 1:
                PlaceDeadEnd(neighbours, position);
                break;

            case 2:
                PlaceTwoWay(neighbours, position);
                break;

            case 3:
                PlaceThreeWay(neighbours, position);
                break;

            case 4:
                Place(position, FOUR_WAY_INTERSECTION);
                break;
        }
    }

    private void Place(Vector3Int position, int roadType)
    {
        placementSystem.PlaceStructureAt(roadType, position);
    }

    private static int CountConnectedRoads(bool[] neighbours)
    {
        int count = 0;
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbours[i]) count++;
        }
        return count;
    }

    private void PlaceTwoWay(bool[] n, Vector3Int pos)
    {
        bool r = n[RIGHT];
        bool l = n[LEFT];
        bool t = n[TOP];
        bool b = n[BOTTOM];

        // Horizontal
        if (r && l)
        {
            int type = pos.x < 0 ? HORIZONTAL_ROAD_LEFT : HORIZONTAL_ROAD_RIGHT;
            Place(pos, type);
            return;
        }

        // Vertical
        if (t && b)
        {
            Place(pos, VERTICAL_ROAD);
            return;
        }

        // Corner
        PlaceCorner(n, pos);
    }

    private void PlaceThreeWay(bool[] n, Vector3Int pos)
    {
        bool r = n[RIGHT];
        bool l = n[LEFT];
        bool t = n[TOP];
        bool b = n[BOTTOM];

        if (t && r && l)
            Place(pos, THREE_WAY_TOP_RIGHT_LEFT);
        else if (t && r && b)
            Place(pos, THREE_WAY_TOP_RIGHT_BOTTOM);
        else if (t && l && b)
            Place(pos, THREE_WAY_TOP_LEFT_BOTTOM);
        else if (b && r && l)
            Place(pos, THREE_WAY_BOTTOM_RIGHT_LEFT);
    }

    private void PlaceCorner(bool[] n, Vector3Int pos)
    {
        bool r = n[RIGHT];
        bool l = n[LEFT];
        bool t = n[TOP];
        bool b = n[BOTTOM];

        if (t && r)
            Place(pos, CORNER_TOP_RIGHT);
        else if (t && l)
            Place(pos, CORNER_TOP_LEFT);
        else if (b && r)
            Place(pos, CORNER_BOTTOM_RIGHT);
        else if (b && l)
            Place(pos, CORNER_BOTTOM_LEFT);
    }

    private void PlaceDeadEnd(bool[] n, Vector3Int pos)
    {
        if (n[RIGHT])
            Place(pos, DEAD_END_LEFT);
        else if (n[LEFT])
            Place(pos, DEAD_END_RIGHT);
        else if (n[TOP])
            Place(pos, DEAD_END_DOWN);
        else if (n[BOTTOM])
            Place(pos, DEAD_END_UP);
    }

    public void FixNeighbouringRoadsAt(IReadOnlyList<Vector3Int> positions)
    {
        foreach (var pos in positions)
        {
            placementSystem.RemoveStructureAt(pos);

            var neighbours = placementSystem.gridData.GetNeighbouringRoads(pos);
            FixRoadAt(pos, neighbours);
        }
    }

    public void FixNeighbouringRoadsAt(List<Vector3Int> positions)
    {
        FixNeighbouringRoadsAt((IReadOnlyList<Vector3Int>)positions);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogRoadConnections(Vector3Int position)
    {
        var roads = placementSystem.gridData.GetNeighbouringRoads(position);

        Debug.Log(
            $"Road connections at {position}: " +
            $"R={roads[RIGHT]}, L={roads[LEFT]}, T={roads[TOP]}, B={roads[BOTTOM]}"
        );
    }
}