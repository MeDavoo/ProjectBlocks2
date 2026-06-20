using UnityEngine;
using UnityEngine.InputSystem;

// Everything about controlling the God lives here: moving the hand with the
// mouse, and later picking up / dragging blocks (that logic gets added into
// Update() below when we get to it). GameManager turns this component on/off
// depending on whether this player is currently in God mode.
public class GodController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera godCamera;
    [SerializeField] private Transform handTransform; // the hand sprite that follows the mouse

    private void Update()
    {
        MoveHandToMouse();

        // --- Later: click-and-drag goes here ---
        // - on mouse down: check what's under the hand, "pick it up"
        // - while holding: keep moving it with the hand (already happens above)
        // - on mouse up: "drop" it
    }

    private void MoveHandToMouse()
    {
        if (handTransform == null || godCamera == null) return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(godCamera.transform.position.z); // distance from camera

        Vector3 mouseWorldPos = godCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = handTransform.position.z; // keep the hand's own depth

        handTransform.position = mouseWorldPos;
    }
}