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
            var power = other.GetComponentInParent<PlayerPowerController>();
            if (power == null) return;
 
            power.AddPower(powerValue);
 
            if (pickupVfx != null)
                Instantiate(pickupVfx, transform.position, Quaternion.identity);
 
            // Swap to pooling later if orbs spawn/despawn a lot.
            Destroy(gameObject);
        }
    }

