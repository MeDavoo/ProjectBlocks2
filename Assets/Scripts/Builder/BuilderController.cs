using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// hand follows mouse, body physics when not carried, recall with C.
// sticks to the first thing he touches when thrown, unsticks after
// stickTime, then falls freely - won't stick again until recalled.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class BuilderController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PlayerController playerController; // for the carry slot

    [Header("Hand")]
    [SerializeField] private Camera builderCamera;
    [SerializeField] private Transform handTransform;

    [Header("Gravity")]
    [SerializeField] private float fallAcceleration = 100f;
    [SerializeField] private float maxFallSpeed = 35f;

    [Header("Stick (Glue)")]
    [Tooltip("How long he stays stuck after hitting a surface, in seconds.")]
    [SerializeField] private float stickTime = 3f;
    [Tooltip("Last X seconds before unsticking, IsStickWarning is true.")]
    [SerializeField] private float warningDuration = 1f;

    [Header("Collision")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float grounderDistance = 0.1f;
    [SerializeField] private float wallCheckDistance = 0.1f;

    [Header("Recall")]
    [SerializeField] private Key recallKey = Key.C;

    [Header("Hand Images")]
    [SerializeField] private Image handImage;

    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite clickSprite;
    [SerializeField] private Sprite hoverSprite;

    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;
    private Vector2 _frameVelocity;

    private bool _grounded;
    private bool _onWallLeft;
    private bool _onWallRight;
    private bool _onCeiling;

    private bool _isCarried = true;
    private bool _isStuck;
    private bool _canStick = true; // goes false after his one stick this flight, reset on recall
    private float _stickTimer;
    private bool _inputEnabled = false;

    private bool _isHoveringBuildArea;

    public bool IsGrounded => _grounded;
    public bool IsCarried => _isCarried;
    public bool IsStuck => _isStuck;

    // true in the last warningDuration seconds before unstick
    // todo: hook a flashing-red sprite/anim to this once it exists
    public bool IsStickWarning => _isStuck && _stickTimer <= warningDuration;

    // for the trajectory preview, so it matches the real physics
    public float FallAcceleration => fallAcceleration;
    public LayerMask GroundLayer => groundLayer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        _rb.gravityScale = 0f;
    }

    private void Start()
    {
        SnapToCarrySlot();
    }

    private void Update()
    {
        if (_inputEnabled)
        {
            MoveHandToMouse();

            UpdateHandSprite();

            if (Keyboard.current != null &&
                Keyboard.current[recallKey].wasPressedThisFrame)
            {
                RecallToPlayer();
            }
        }

        if (_isStuck)
        {
            _stickTimer -= Time.deltaTime;

            if (_stickTimer <= 0f)
            {
                Unstick();
            }
        }

        // later: click-and-drag / placing structures goes here
    }

    private void FixedUpdate()
    {
        if (_isCarried || _isStuck) return;

        CheckCollisions();

        if (_canStick && (_grounded || _onWallLeft || _onWallRight || _onCeiling))
        {
            StickToSurface();
            return;
        }

        HandleGravity();
        ApplyMovement();
    }

    // hand
    private void MoveHandToMouse()
    {
        if (handTransform == null || builderCamera == null) return;

        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        mouseScreenPos.z = Mathf.Abs(builderCamera.transform.position.z);

        Vector3 mouseWorldPos = builderCamera.ScreenToWorldPoint(mouseScreenPos);
        mouseWorldPos.z = handTransform.position.z;

        handTransform.position = mouseWorldPos;
    }

    private void UpdateHandSprite()
    {
        if (handImage == null)
            return;

        if (_isHoveringBuildArea)
        {
            handImage.sprite = hoverSprite;
            return;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.isPressed)
        {
            handImage.sprite = clickSprite;
            return;
        }

        handImage.sprite = normalSprite;
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    public void SetHoveringBuildArea(bool hovering)
    {
        _isHoveringBuildArea = hovering;
    }

    // collisions
    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        _grounded = Physics2D.CapsuleCast(
            _col.bounds.center, _col.size, _col.direction, 0f,
            Vector2.down, grounderDistance, groundLayer);

        _onCeiling = Physics2D.CapsuleCast(
            _col.bounds.center, _col.size, _col.direction, 0f,
            Vector2.up, grounderDistance, groundLayer);

        _onWallLeft = Physics2D.CapsuleCast(
            _col.bounds.center, _col.size, _col.direction, 0f,
            Vector2.left, wallCheckDistance, groundLayer);

        _onWallRight = Physics2D.CapsuleCast(
            _col.bounds.center, _col.size, _col.direction, 0f,
            Vector2.right, wallCheckDistance, groundLayer);

        Physics2D.queriesStartInColliders = true;
    }

    // gravity - only runs while airborne, sticking handles landing
    private void HandleGravity()
    {
        _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -maxFallSpeed, fallAcceleration * Time.fixedDeltaTime);
    }

    private void ApplyMovement()
    {
        _rb.linearVelocity = _frameVelocity;
    }

    // stick / glue
    private void StickToSurface()
    {
        _isStuck = true;
        _frameVelocity = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        _stickTimer = stickTime;

        // todo: trigger a stuck/landed animation here if wanted
    }

    private void Unstick()
    {
        _isStuck = false;
        _canStick = false; // used up his one stick for this flight
        _rb.bodyType = RigidbodyType2D.Dynamic;

        // todo: stop the warning animation here
    }

    // carry / recall
    // brings him home from wherever he is, no-op if already carried
    public void RecallToPlayer()
    {
        if (_isCarried) return;

        SnapToCarrySlot();

        if (!GameManager.IsMultiplayer && gameManager != null)
        {
            gameManager.SetFocus(GameManager.Focus.Player);
        }
    }

    private void SnapToCarrySlot()
    {
        if (playerController == null || playerController.BuilderCarrySlot == null)
        {
            Debug.LogWarning("BuilderController needs a carry slot on the Player - assign Player Controller.", this);
            return;
        }

        _isCarried = true;
        _isStuck = false;
        _canStick = true; // recalled - next throw can stick again
        _frameVelocity = Vector2.zero;
        _rb.linearVelocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;

        transform.SetParent(playerController.BuilderCarrySlot);
        transform.localPosition = Vector3.zero;

        playerController.SetCarryingBuilder(true);
    }

    // throw hook - gives him a starting velocity, gravity + sticking take over
    public void Launch(Vector2 velocity)
    {
        _isCarried = false;
        _isStuck = false;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        transform.SetParent(null);
        _frameVelocity = velocity;

        if (playerController != null)
            playerController.SetCarryingBuilder(false);
    }
}