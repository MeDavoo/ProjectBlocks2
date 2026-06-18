using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Holds the queue of upcoming blocks ("tetris conveyor belt"). Both
/// God and Player can see this, so this is meant to be read by UI on
/// both sides regardless of who's currently in control.
/// </summary>
public class BlockConveyor : MonoBehaviour
{
    [Header("Pool of possible shapes")]
    [SerializeField] private List<BlockShapeSO> possibleShapes = new();

    [Header("Checkpoint Rules")]
    [SerializeField] private BlockShapeSO checkpointShape; // guaranteed every N blocks
    [SerializeField] private int checkpointEvery = 50;
    [SerializeField] private bool checkpointsEnabled = true; // turn off for Nightmare mode

    [Header("Queue")]
    [SerializeField] private int previewCount = 3;

    private readonly Queue<BlockShapeSO> _queue = new();
    private int _blocksPlacedSinceCheckpoint;

    /// <summary>Fired whenever the queue changes (consume, refill, etc).</summary>
    public event Action OnConveyorChanged;

    public BlockShapeSO CurrentBlock => _queue.Count > 0 ? _queue.Peek() : null;

    private void Awake()
    {
        for (int i = 0; i < previewCount; i++)
        {
            _queue.Enqueue(GetNextShape());
        }
    }

    /// <summary>Snapshot of upcoming blocks for UI display, front to back.</summary>
    public List<BlockShapeSO> GetUpcoming()
    {
        return _queue.ToList();
    }

    /// <summary>
    /// Called by the God hand controller after a block has been
    /// successfully placed and paid for.
    /// </summary>
    public void ConsumeCurrent()
    {
        if (_queue.Count == 0) return;

        BlockShapeSO consumed = _queue.Dequeue();
        if (consumed.isCheckpointBlock)
        {
            _blocksPlacedSinceCheckpoint = 0;
        }
        else
        {
            _blocksPlacedSinceCheckpoint++;
        }

        _queue.Enqueue(GetNextShape());
        OnConveyorChanged?.Invoke();
    }

    private BlockShapeSO GetNextShape()
    {
        if (checkpointsEnabled && checkpointShape != null && _blocksPlacedSinceCheckpoint >= checkpointEvery)
        {
            // Don't reset the counter here — it resets only once the
            // checkpoint block is actually consumed/placed, so it stays
            // "guaranteed next" until then.
            return checkpointShape;
        }

        if (possibleShapes.Count == 0)
        {
            Debug.LogWarning("BlockConveyor has no possibleShapes assigned.");
            return null;
        }

        return possibleShapes[UnityEngine.Random.Range(0, possibleShapes.Count)];
    }
}