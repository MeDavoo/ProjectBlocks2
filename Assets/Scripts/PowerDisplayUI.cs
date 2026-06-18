using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerDisplayUI : MonoBehaviour
{
    [SerializeField] private PlayerPowerController power;

    [Header("Optional - assign whichever you're using")]
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private Slider powerBar;

    private void OnEnable()
    {
        if (power == null) return;

        power.OnPowerChanged += HandlePowerChanged;
        HandlePowerChanged(power.CurrentPower, power.MaxPower);
    }

    private void OnDisable()
    {
        if (power == null) return;
        power.OnPowerChanged -= HandlePowerChanged;
    }

    private void HandlePowerChanged(float current, float max)
    {
        if (powerText != null)
        {
            powerText.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        }

        if (powerBar != null)
        {
            powerBar.maxValue = max;
            powerBar.value = current;
        }
    }
}