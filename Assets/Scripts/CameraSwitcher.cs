using UnityEngine;
using UnityEngine.InputSystem;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField]
    private InputAction action;
    private Animator animator;
    private bool playerCamera = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    private void Start()
    {
        action.performed += _ => SwitchCamera();
    }

    private void SwitchCamera()
    {
        if (playerCamera)
        {
            animator.Play("GodCamera");
        }
        else
        {
            animator.Play("PlayerCamera");
        }
        playerCamera = !playerCamera;
    }
}
