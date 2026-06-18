using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// The grid the God places blocks into. Coordinates are taken straight
/// from a reference Tilemap (your PlatformGrid > Tilemap), so cells line
/// up exactly with the platforms the Player walks on, and any cell that
/// already has a platform tile is automatically blocked.
/// </summary>
public class BlockGrid : MonoBehaviour
{
    [Header("Sync Source")]
    [Tooltip("Drag the PlatformGrid's Tilemap here. Cell size/position and existing platform tiles come from this.")]
    [SerializeField] private Tilemap platformTilemap;

    [Header("Placement Area (cell coordinates, same space as the tilemap)")]
    [Tooltip("Where the God is allowed to place blocks. Use the Tilemap's cell coordinates — " +
             "select the Tilemap in the editor and check its Cell Bounds in the inspector to find sensible numbers, " +
             "then extend upward (+y) to cover empty air above the platforms where blocks can go.")]
    [SerializeField] private BoundsInt placementBounds = new BoundsInt(-10, 0, 0, 40, 20, 1);

    [Header("Visuals")]
    [SerializeField] private GameObject defaultCellVisualPrefab; // simple sprite quad, will be scaled to tile size
    [SerializeField] private Transform placedBlocksParent;

    // Cells the God cannot place into — pre-filled from the platform tilemap, then added to as blocks are placed.
    private readonly HashSet<Vector3Int> _occupied = new();
    private readonly Dictionary<Vector3Int, GameObject> _cellVisuals = new();

    public Vector3 CellSize => platformTilemap != null ? platformTilemap.cellSize : Vector3.one;

    private void Awake()
    {
        if (placedBlocksParent == null)
        {
            var go = new GameObject("PlacedBlocks");
            go.transform.SetParent(transform);
            placedBlocksParent = go.transform;
        }

        RefreshBlockedCellsFromTilemap();
    }

    /// <summary>
    /// Scans the platform tilemap and marks every cell inside
    /// placementBounds that already has a tile as occupied. Call again
    /// at runtime if the platform tilemap changes (e.g. procedural
    /// generation reveals new chunks).
    /// </summary>
    public void RefreshBlockedCellsFromTilemap()
    {
        _occupied.Clear();

        if (platformTilemap == null)
        {
            Debug.LogWarning("BlockGrid has no platformTilemap assigned — nothing will be blocked by terrain.", this);
            return;
        }

        foreach (var pos in placementBounds.allPositionsWithin)
        {
            if (platformTilemap.HasTile(pos))
            {
                _occupied.Add(pos);
            }
        }
    }

    public Vector3Int WorldToGridCoord(Vector3 worldPos)
    {
        return platformTilemap != null ? platformTilemap.WorldToCell(worldPos) : Vector3Int.zero;
    }

    public Vector3 GridToWorldCenter(Vector3Int coord)
    {
        return platformTilemap != null ? platformTilemap.GetCellCenterWorld(coord) : Vector3.zero;
    }

    public bool InBounds(Vector3Int coord) => placementBounds.Contains(coord);

    public bool IsOccupied(Vector3Int coord)
    {
        if (!InBounds(coord)) return true; // treat out-of-bounds as blocked
        return _occupied.Contains(coord);
    }

    /// Checks whether ALL cells of a shape (relative cells + a grid
    /// origin, in the same cell space as the tilemap) are free, in
    /// bounds, and not already covered by a platform tile.

    public bool CanPlaceShape(Vector2Int[] relativeCells, Vector3Int originCoord)
    {
        foreach (var rel in relativeCells)
        {
            Vector3Int coord = originCoord + new Vector3Int(rel.x, rel.y, 0);
            if (!InBounds(coord)) return false;
            if (_occupied.Contains(coord)) return false;
        }
        return true;
    }

    /// Places a shape into the grid, marking cells occupied and spawning
    /// visuals. Caller is responsible for checking CanPlaceShape and
    /// spending power BEFORE calling this.
    public void PlaceShape(Vector2Int[] relativeCells, Vector3Int originCoord, BlockShapeSO shapeData)
    {
        GameObject prefab = shapeData.cellVisualPrefabOverride != null
            ? shapeData.cellVisualPrefabOverride
            : defaultCellVisualPrefab;

        foreach (var rel in relativeCells)
        {
            Vector3Int coord = originCoord + new Vector3Int(rel.x, rel.y, 0);
            if (!InBounds(coord)) continue;

            _occupied.Add(coord);

            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab, GridToWorldCenter(coord), Quaternion.identity, placedBlocksParent);
                visual.transform.localScale = CellSize;

                var sr = visual.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) sr.color = shapeData.blockColor;

                _cellVisuals[coord] = visual;
            }
        }
    }

    /// <summary>Clears a single cell (e.g. block destroyed by lava / hazard).</summary>
    public void ClearCell(Vector3Int coord)
    {
        _occupied.Remove(coord);

        if (_cellVisuals.TryGetValue(coord, out var visual))
        {
            Destroy(visual);
            _cellVisuals.Remove(coord);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (platformTilemap == null) return;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Vector3 min = platformTilemap.CellToWorld(placementBounds.min);
        Vector3 max = platformTilemap.CellToWorld(placementBounds.max);
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        Gizmos.DrawWireCube(center, size);
    }
}