using System;
using UnityEngine;
using UnityEngine.InputSystem;

    public enum GameMode
    {
        Player,
        God
    }
 
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }
 
        [Header("Camera (same Animator your CameraSwitcher used)")]
        [SerializeField] private Animator cameraAnimator;
        [SerializeField] private string playerCameraStateName = "PlayerCamera";
        [SerializeField] private string godCameraStateName = "GodCamera";
 
        [Header("Controllers to enable/disable")]
        [SerializeField] private MonoBehaviour playerMovementController;
        [SerializeField] private MonoBehaviour godHandController;
 
        [Header("Input")]
        [SerializeField] private InputAction toggleAction;
 
        public GameMode CurrentMode { get; private set; }
 
        public event Action<GameMode> OnModeChanged;
 
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
 
        private void OnEnable()
        {
            toggleAction.Enable();
            toggleAction.performed += OnTogglePerformed;
        }
 
        private void OnDisable()
        {
            toggleAction.performed -= OnTogglePerformed;
            toggleAction.Disable();
        }
 
        private void Start()
        {
            SetMode(GameMode.Player);
        }
 
        private void OnTogglePerformed(InputAction.CallbackContext ctx)
        {
            ToggleMode();
        }
 
        public void ToggleMode()
        {
            SetMode(CurrentMode == GameMode.Player ? GameMode.God : GameMode.Player);
        }
 
        public void SetMode(GameMode mode)
        {
            CurrentMode = mode;
            bool isGod = mode == GameMode.God;
 
            if (cameraAnimator != null)
            {
                cameraAnimator.Play(isGod ? godCameraStateName : playerCameraStateName);
            }
 
            if (playerMovementController != null) playerMovementController.enabled = !isGod;
            if (godHandController != null) godHandController.enabled = isGod;
 
            OnModeChanged?.Invoke(mode);
        }
    }