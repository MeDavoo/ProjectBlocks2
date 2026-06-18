using UnityEngine;

/// <summary>
/// Defines one block "piece" — its grid cells (relative to a local
/// origin), its power cost, and how it looks. Make one asset per
/// tetromino-like shape (I, O, T, L, S, Z, single-cell, checkpoint, etc).
///
/// Right-click in Project window -> Create -> ProjectBlocks -> Block Shape
/// </summary>
[CreateAssetMenu(menuName = "ProjectBlocks/Block Shape", fileName = "NewBlockShape")]
public class BlockShapeSO : ScriptableObject
{
    [Header("Identity")]
    public string shapeId = "I";
    public Sprite uiIcon; // shown in the conveyor UI slot

    [Header("Shape")]
    [Tooltip("Cells relative to (0,0). E.g. a horizontal I-piece: (0,0),(1,0),(2,0),(3,0)")]
    public Vector2Int[] cells = new[] { new Vector2Int(0, 0) };

    [Header("Cost")]
    public float powerCost = 10f;

    [Header("Visuals")]
    public Color blockColor = Color.white;
    [Tooltip("Optional prefab used per-cell when placed in the world. If null, BlockGrid uses its default cell prefab.")]
    public GameObject cellVisualPrefabOverride;

    [Header("Gameplay")]
    public bool isCheckpointBlock = false;

    /// <summary>
    /// Returns this shape's cells rotated clockwise by 90*steps degrees,
    /// then normalized so the minimum x/y is 0 (keeps grid-origin math simple).
    /// </summary>
    public Vector2Int[] GetRotatedCells(int steps)
    {
        steps = ((steps % 4) + 4) % 4;
        var result = new Vector2Int[cells.Length];

        for (int i = 0; i < cells.Length; i++)
        {
            Vector2Int c = cells[i];
            for (int s = 0; s < steps; s++)
            {
                // 90 degree clockwise rotation: (x, y) -> (y, -x)
                c = new Vector2Int(c.y, -c.x);
            }
            result[i] = c;
        }

        // Normalize so the shape's min corner sits at (0,0)
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in result)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
        }
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = new Vector2Int(result[i].x - minX, result[i].y - minY);
        }

        return result;
    }
}