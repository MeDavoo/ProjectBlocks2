using UnityEngine;
using UnityEngine.InputSystem;

// The single source of truth for "is this player currently Player or God."
// Press Tab to switch. This is what decides which controller script gets to
// run, and swaps the Cinemachine camera to match.
//
// Later (multiplayer): each player would get their own GameManager +
// GodController + movement script. The only extra rule needed at that point
// is "both players can't be God at once" — that's a small check to add to
// OnTogglePressed later, not a redesign.
public class GameManager : MonoBehaviour
{
    public enum Mode { Player, God }

    [Header("Camera (Cinemachine State-Driven Camera's Animator)")]
    [SerializeField] private Animator cameraAnimator;

    [Header("Controllers")]
    [SerializeField] private GodController godController;
    [SerializeField] private PlayerController playerController;

    [Header("UI (swaps based on mode)")]
    [SerializeField] private GameObject playerGUI;
    [SerializeField] private GameObject godGUI;

    [Header("Input")]
    [SerializeField] private InputAction toggleAction;

    public Mode CurrentMode { get; private set; } = Mode.Player;

    private void OnEnable()
    {
        toggleAction.Enable();
        toggleAction.performed += OnTogglePressed;
    }

    private void OnDisable()
    {
        toggleAction.performed -= OnTogglePressed;
        toggleAction.Disable();
    }

    private void Start()
    {
        ApplyMode();
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        CurrentMode = CurrentMode == Mode.Player ? Mode.God : Mode.Player;
        ApplyMode();
    }

    private void ApplyMode()
    {
        bool isGod = CurrentMode == Mode.God;

        cameraAnimator.Play(isGod ? "GodCamera" : "PlayerCamera");

        if (playerController != null)
            playerController.SetMovementLocked(isGod);

        if (godController != null)
            godController.enabled = isGod;

        if (playerGUI != null) playerGUI.SetActive(!isGod);
        if (godGUI != null) godGUI.SetActive(isGod);
    }
}