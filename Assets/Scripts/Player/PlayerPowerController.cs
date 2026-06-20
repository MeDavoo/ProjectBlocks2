using System;
using UnityEngine;

    public class PlayerPowerController : MonoBehaviour
    {
        [Header("Power Settings")]
        [SerializeField] private float maxPower = 100f;
        [SerializeField] private float startingPower = 50f;
 
        public float MaxPower => maxPower;
        public float CurrentPower { get; private set; }
        public event Action<float, float> OnPowerChanged;
 
        private void Awake()
        {
            CurrentPower = Mathf.Clamp(startingPower, 0f, maxPower);
        }
 
        public void AddPower(float amount)
        {
            if (amount <= 0f) return;
            CurrentPower = Mathf.Clamp(CurrentPower + amount, 0f, maxPower);
            OnPowerChanged?.Invoke(CurrentPower, maxPower);
        }
 
        public bool HasEnough(float amount) => CurrentPower >= amount;
 

        public bool TrySpend(float amount)
        {
            if (amount <= 0f) return true;
            if (CurrentPower < amount) return false;
 
            CurrentPower -= amount;
            OnPowerChanged?.Invoke(CurrentPower, maxPower);
            return true;
        }
 
        public void SetMax(float newMax)
        {
            maxPower = newMax;
            CurrentPower = Mathf.Clamp(CurrentPower, 0f, maxPower);
            OnPowerChanged?.Invoke(CurrentPower, maxPower);
        }
    }