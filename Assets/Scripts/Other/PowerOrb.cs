using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class PowerOrb : MonoBehaviour
{
    [SerializeField] private float powerValue = 5f;
    [SerializeField] private GameObject pickupVfx;
 
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }
 
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the Runner should be able to pick these up.
        bool isPlayer = other.GetComponentInParent<PlayerController>() != null;
        if (!isPlayer) return;
 
        if (BuilderPowerController.Instance == null)
        {
            Debug.LogWarning("PowerOrb collected, but no BuilderPowerController found in the scene.");
            return;
        }
 
        BuilderPowerController.Instance.AddPower(powerValue);
 
        if (pickupVfx != null)
            Instantiate(pickupVfx, transform.position, Quaternion.identity);
 
        // Swap to pooling later if orbs spawn/despawn a lot.
        Destroy(gameObject);
    }
}
