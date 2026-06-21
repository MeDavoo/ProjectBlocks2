using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Unity.Cinemachine;

// Solo testing lets one person flip between Player and Builder with Tab.
// Real co-op doesn't — each person stays in their own role the whole game.
//
// IsMultiplayer is exposed as a static so ANY script can check it without
// needing a direct reference to this GameManager
public class GameManager : MonoBehaviour
{
    public enum Focus { Player, Builder }

    [Header("Mode")]
    [Tooltip("OFF = solo testing, Tab swaps focus between Player and Builder.\nON = real co-op, each player stays in their own role, Tab does nothing.")]
    [SerializeField] private bool isMultiplayer = false;

    // Mirrors isMultiplayer so other scripts can check it without a reference to this object.
    public static bool IsMultiplayer { get; private set; }

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera builderCamera;

    [Header("Controllers")]
    [SerializeField] private PlayerController playerController;   // the Player's movement
    [SerializeField] private BuilderController builderController; // the Builder's hand/mouse control

    [Header("UI (swaps based on focus)")]
    [SerializeField] private GameObject playerGUI;
    [SerializeField] private GameObject builderGUI;

    [Header("Input (solo only)")]
    [SerializeField] private InputAction toggleAction;

    [Header("World Objects")]
    [SerializeField] private GameObject builderGridOverlay;
    public Focus CurrentFocus { get; private set; } = Focus.Player;

    private void Awake()
    {
        IsMultiplayer = isMultiplayer;
    }

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
        ApplyFocus();
    }

    private void OnTogglePressed(InputAction.CallbackContext ctx)
    {
        if (isMultiplayer) return; // real co-op: roles are fixed, ignore Tab entirely

        CurrentFocus = CurrentFocus == Focus.Player ? Focus.Builder : Focus.Player;
        Debug.Log($"Current Focus = {CurrentFocus}");
        ApplyFocus();
    }

    public void SetFocus(Focus focus)
    {
        if (isMultiplayer) return;

        CurrentFocus = focus;
        ApplyFocus();
    }

    private void ApplyFocus()
    {
        bool builderFocused = CurrentFocus == Focus.Builder;

        playerCamera.Priority = builderFocused ? 0 : 100;
        builderCamera.Priority = builderFocused ? 100 : 0;

        playerController.SetMovementLocked(builderFocused);
        builderController.SetInputEnabled(builderFocused);

        playerGUI.SetActive(!builderFocused);
        builderGUI.SetActive(builderFocused);

        builderGridOverlay.SetActive(builderFocused);
    }
}