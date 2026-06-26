using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    public bool Walkable = true;
    public Unit Occupant { get; set; }

    [SerializeField] private SpriteRenderer highlightRenderer;

    // Colors
    private static readonly Color BaseColor = new Color(1f, 1f, 1f, 0f);

    private static readonly Color MoveColor = new Color(1f, 1f, 0.2f, 0.28f);
    private static readonly Color AttackColor = new Color(1f, 0.15f, 0.15f, 0.32f);
    private static readonly Color ReadyColor = new Color(0.2f, 0.55f, 1f, 0.28f);
    private static readonly Color DeployColor = new Color(0.2f, 1f, 0.25f, 0.25f);

    private bool moveHighlighted;
    private bool attackHighlighted;
    private bool readyHighlighted; // 🔹 NEW

    public bool IsPlayerDeployTile; // set by DeploymentManager at runtime (MVP)

    private bool deployHighlighted;

    public void Init(Vector2Int coord)
    {
        Coord = coord;
        ApplyColor();
    }

    // Backwards-compatible: your old code still works
    public void SetHighlight(bool on)
    {
        moveHighlighted = on;
        ApplyColor();
    }
    public void SetDeployHighlight(bool on)
    {
        deployHighlighted = on;
        ApplyColor();
    }
    public void SetAttackHighlight(bool on)
    {
        attackHighlighted = on;
        ApplyColor();
    }

    // 🔹 NEW: blue “can still act” highlight
    public void SetReadyHighlight(bool on)
    {
        readyHighlighted = on;
        ApplyColor();
    }


    private void ApplyColor()
    {
        if (highlightRenderer == null)
        {
            Debug.LogError($"{name}: Highlight Renderer is not assigned.");
            return;
        }

        if (attackHighlighted) highlightRenderer.color = AttackColor;
        else if (moveHighlighted) highlightRenderer.color = MoveColor;
        else if (readyHighlighted) highlightRenderer.color = ReadyColor;
        else if (deployHighlighted) highlightRenderer.color = DeployColor;
        else highlightRenderer.color = BaseColor;
    }

    public bool IsWalkable { get; private set; } = true;

    public void SetWalkable(bool value)
    {
        Walkable = value;
    }
}