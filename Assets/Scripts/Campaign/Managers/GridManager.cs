using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private int width = 10;
    [SerializeField] private int height = 8;
    [SerializeField] private float cellSize = 1f;

    [SerializeField] private LayerMask obstacleLayer;

    [Header("Grid Origin")]
    [SerializeField] private bool centerGridOnThisObject = true;

    private Dictionary<Vector2Int, Tile> tiles = new();

    public Tile GetTile(Vector2Int coord) =>
        tiles.TryGetValue(coord, out var t) ? t : null;

    private Vector3 GetGridOrigin()
    {
        if (!centerGridOnThisObject)
            return transform.position;

        float xOffset = (width - 1) * cellSize * 0.5f;
        float yOffset = (height - 1) * cellSize * 0.5f;

        return transform.position - new Vector3(xOffset, yOffset, 0f);
    }

    public Tile GetTileFromWorld(Vector3 worldPos)
    {
        Vector3 origin = GetGridOrigin();
        Vector3 local = worldPos - origin;

        int x = Mathf.RoundToInt(local.x / cellSize);
        int y = Mathf.RoundToInt(local.y / cellSize);

        return GetTile(IndexToCoord(x, y));
    }

    public void RebuildGrid()
    {
        BuildGrid();
    }

    private void BuildGrid()
    {
        tiles.Clear();

        Vector3 origin = GetGridOrigin();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var coord = IndexToCoord(x, y);

                Vector3 worldPos = origin + new Vector3(
                    x * cellSize,
                    y * cellSize,
                    0f
                );

                var tile = Instantiate(
                    tilePrefab,
                    worldPos,
                    Quaternion.identity,
                    transform
                );

                tile.name = $"Tile_{x}_{y}";
                tile.Init(coord);

                bool hasObstacle = Physics2D.OverlapBox(
                    worldPos,
                    new Vector2(cellSize * 0.8f, cellSize * 0.8f),
                    0f,
                    obstacleLayer
                );

                tile.SetWalkable(!hasObstacle);

                tiles[coord] = tile;
            }
        }
    }

    public IEnumerable<Tile> GetNeighbors(Tile t)
    {
        var c = t.Coord;

        var dirs = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (var d in dirs)
        {
            var n = GetTile(c + d);
            if (n != null)
                yield return n;
        }
    }

    [SerializeField] private bool useCenteredCoordinates = true;

    private Vector2Int IndexToCoord(int x, int y)
    {
        if (!useCenteredCoordinates)
            return new Vector2Int(x, y);

        return new Vector2Int(
            x - width / 2,
            y - height / 2
        );
    }
}