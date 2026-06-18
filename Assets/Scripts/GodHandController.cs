using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drives the floating God hand: follows the mouse, picks up a block
/// from the conveyor (via ConveyorSlotUI.OnClick -> PickUpBlock),
/// shows a snapped ghost preview over the grid, rotates with a key,
/// and places (spending power) on click.
/// </summary>
public class GodHandController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera godCamera;
    [SerializeField] private BlockGrid grid;
    [SerializeField] private BlockConveyor conveyor;
    [SerializeField] private PlayerPowerController power;
    [SerializeField] private Transform handTransform; // the visible hand sprite/rig that follows the mouse

    [Header("Ghost Preview")]
    [SerializeField] private GameObject ghostCellPrefab; // simple sprite quad
    [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 0.6f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("Input")]
    [SerializeField] private KeyCode rotateKey = KeyCode.R;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    [SerializeField] private int placeMouseButton = 0;  // left click
    [SerializeField] private int cancelMouseButton = 1; // right click

    private BlockShapeSO _heldShape;
    private int _rotationSteps;
    private readonly List<GameObject> _ghostCells = new();
    private Vector3Int _currentOriginCoord;
    private bool _currentPlacementValid;

    public bool IsHolding => _heldShape != null;

    private void Update()
    {
        UpdateHandPosition();

        if (!IsHolding) return;

        if (Input.GetKeyDown(rotateKey))
        {
            _rotationSteps = (_rotationSteps + 1) % 4;
        }

        UpdateGhostPreview();

        if (Input.GetMouseButtonDown(placeMouseButton))
        {
            TryPlaceHeldBlock();
        }
        else if (Input.GetMouseButtonDown(cancelMouseButton) || Input.GetKeyDown(cancelKey))
        {
            CancelHold();
        }
    }

    private void UpdateHandPosition()
    {
        if (handTransform == null || godCamera == null) return;

        Vector3 mouseWorld = GetMouseWorldPosition();
        mouseWorld.z = handTransform.position.z; // keep hand's own depth
        handTransform.position = mouseWorld;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPos = Input.mousePosition;
        // For an orthographic 2D camera this just needs *some* z distance from camera.
        screenPos.z = Mathf.Abs(godCamera.transform.position.z);
        return godCamera.ScreenToWorldPoint(screenPos);
    }

    /// <summary>
    /// Call this from ConveyorSlotUI's OnClick (pass the shape that slot represents).
    /// Only works on the front-of-queue slot in practice, but we don't
    /// enforce that here — ConveyorSlotUI decides what's clickable.
    /// </summary>
    public void PickUpBlock(BlockShapeSO shape)
    {
        if (shape == null || IsHolding) return;

        _heldShape = shape;
        _rotationSteps = 0;
        SpawnGhostCells();
    }

    private void SpawnGhostCells()
    {
        ClearGhostVisuals();

        if (ghostCellPrefab == null || _heldShape == null) return;

        foreach (var _ in _heldShape.cells)
        {
            GameObject cell = Instantiate(ghostCellPrefab, transform);
            cell.transform.localScale = grid.CellSize;
            _ghostCells.Add(cell);
        }
    }

    private void UpdateGhostPreview()
    {
        if (_heldShape == null || _ghostCells.Count == 0) return;

        Vector2Int[] rotatedCells = _heldShape.GetRotatedCells(_rotationSteps);

        // Anchor the shape's local (0,0) cell under the hand position.
        _currentOriginCoord = grid.WorldToGridCoord(handTransform.position);
        _currentPlacementValid = grid.CanPlaceShape(rotatedCells, _currentOriginCoord)
                                  && power.HasEnough(_heldShape.powerCost);

        Color color = _currentPlacementValid ? validColor : invalidColor;

        for (int i = 0; i < rotatedCells.Length && i < _ghostCells.Count; i++)
        {
            Vector3Int coord = _currentOriginCoord + new Vector3Int(rotatedCells[i].x, rotatedCells[i].y, 0);
            _ghostCells[i].transform.position = grid.GridToWorldCenter(coord);

            var sr = _ghostCells[i].GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }
    }

    private void TryPlaceHeldBlock()
    {
        if (_heldShape == null) return;

        if (!_currentPlacementValid)
        {
            // TODO: hook up a denial sound/flash here.
            return;
        }

        if (!power.TrySpend(_heldShape.powerCost))
        {
            return; // power changed since last check (edge case) — bail safely
        }

        Vector2Int[] rotatedCells = _heldShape.GetRotatedCells(_rotationSteps);
        grid.PlaceShape(rotatedCells, _currentOriginCoord, _heldShape);

        conveyor.ConsumeCurrent();
        CancelHold();
    }

    private void CancelHold()
    {
        _heldShape = null;
        _rotationSteps = 0;
        ClearGhostVisuals();
    }

    private void ClearGhostVisuals()
    {
        foreach (var cell in _ghostCells)
        {
            if (cell != null) Destroy(cell);
        }
        _ghostCells.Clear();
    }

    private void OnDisable()
    {
        // Switching back to Player mode mid-hold cancels the pickup cleanly.
        CancelHold();
    }
}