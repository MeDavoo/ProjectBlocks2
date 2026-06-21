using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float acceleration = 120f;
    [SerializeField] private float groundDeceleration = 75f;
    [SerializeField] private float airDeceleration = 30f;

    [Header("Sprint")]
    [SerializeField] private float sprintMultiplier = 1.5f;

    [Header("Jump")]
    [SerializeField] private float jumpPower = 30f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBuffer = 0.12f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private float wallSlideSpeed = 5f;
    [SerializeField] private float wallJumpHorizontalForce = 15f;
    [SerializeField] private float wallJumpVerticalForce = 25f;

    [Header("Gravity")]
    [SerializeField] private float fallAcceleration = 100f;
    [SerializeField] private float maxFallSpeed = 35f;
    [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
    [SerializeField] private float groundingForce = -1.5f;

    [Header("Collision")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float grounderDistance = 0.1f;

    [Header("Builder Carry")]
    [Tooltip("Empty child Transform positioned on the Player's back — the Builder snaps here while being carried.")]
    [SerializeField] private Transform builderCarrySlot;

    [Header("Builder Throw")]
    [SerializeField] private BuilderController builderController;
    [SerializeField] private Key throwKey = Key.G;
    [Tooltip("Throw strength with a tap (held for ~0 seconds).")]
    [SerializeField] private float minThrowForce = 8f;
    [Tooltip("Throw strength at full charge.")]
    [SerializeField] private float maxThrowForce = 25f;
    [Tooltip("How long you need to hold the throw key to reach max force.")]
    [SerializeField] private float maxChargeTime = 1.2f;
    [Tooltip("Degrees above horizontal the Builder gets tossed — 0 = straight forward, 90 = straight up.")]
    [SerializeField] private float throwAngle = 45f;

    // Components
    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;

    // State
    private Vector2 _frameVelocity;
    private Vector2 _moveInput;
    private bool _jumpHeld;
    private bool _sprinting;
    private bool _onWall;

    // Jump internals
    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;
    private float _timeLeftGround;
    private int _wallDirection;
    private bool _grounded;
    private float _time;
    private bool _movementLocked;

    // Throw internals
    private int _facingDirection = 1; // 1 = right, -1 = left
    private float _throwCharge;

    // Helpers
    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + jumpBuffer;
    private bool CanUseCoyote   => _coyoteUsable && !_grounded && _time < _timeLeftGround + coyoteTime;

    // ── Public read-only state ──────────────────────────────────────────
    public bool IsGrounded => _grounded;
    public Vector2 Velocity => _rb.linearVelocity;
    public int FacingDirection => _facingDirection;

    // 0-1, how charged the current throw is — handy later for a charge-bar UI.
    public float ThrowChargeRatio => Mathf.Clamp01(_throwCharge / maxChargeTime);

    // Builder carry — useful later for animations (e.g. a "carrying" pose/anim trigger).
    public Transform BuilderCarrySlot => builderCarrySlot;
    public bool IsCarryingBuilder { get; private set; } = true; // Builder starts the game riding on the Player's back

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();
        _rb.gravityScale = 0f; //we aint using unity default gravity, so disabled
    }

    // called by BuilderController when recalled or thrown
    public void SetCarryingBuilder(bool isCarried)
    {
        IsCarryingBuilder = isCarried;
        // todo: hook animation trigger here, e.g. animator.SetBool("CarryingBuilder", isCarried);
    }

    // ── Unity Input System ────────────────────────────────────────

    public void OnMove(InputValue value)
    {
        if (_movementLocked)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (_movementLocked)
        {
            return;
        }

        if (value.isPressed)
        {
            _jumpToConsume = true;
            _timeJumpWasPressed = _time;
            _jumpHeld = true;
        }
        else
        {
            _jumpHeld = false;
        }
    }

    // ── Main loop ───────────────────────────────────────────────────────
    private void Update()
    {
        _time += Time.deltaTime;
        HandleThrowInput();
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        HandleJump();
        HandleHorizontal();
        HandleGravity();
        ApplyMovement();
    }

    // ── Builder Throw ────────────────────────────────────────────────────
    private void HandleThrowInput()
    {
        if (_movementLocked || Keyboard.current == null) return;

        bool keyHeld = Keyboard.current[throwKey].isPressed;
        bool keyReleased = Keyboard.current[throwKey].wasReleasedThisFrame;

        if (keyHeld && IsCarryingBuilder)
        {
            _throwCharge = Mathf.Min(_throwCharge + Time.deltaTime, maxChargeTime);
        }

        if (keyReleased)
        {
            if (_throwCharge > 0f && IsCarryingBuilder)
            {
                ThrowBuilder();
            }
            _throwCharge = 0f;
        }
    }

    private void ThrowBuilder()
    {
        if (builderController == null) return;

        builderController.Launch(ComputeThrowVelocity(ThrowChargeRatio));

        if (!GameManager.IsMultiplayer && gameManager != null)
        {
            gameManager.SetFocus(GameManager.Focus.Builder);
        }
    }

    // throw velocity at a given charge ratio, no actual throw - used by
    // the real throw and the trajectory preview so they stay in sync
    private Vector2 ComputeThrowVelocity(float chargeRatio)
    {
        float force = Mathf.Lerp(minThrowForce, maxThrowForce, chargeRatio);

        float angleRad = throwAngle * Mathf.Deg2Rad;
        Vector2 throwDir = new Vector2(Mathf.Cos(angleRad) * _facingDirection, Mathf.Sin(angleRad));

        return throwDir * force;
    }

    // current would-be throw velocity, for the trajectory preview
    public Vector2 PreviewThrowVelocity => ComputeThrowVelocity(ThrowChargeRatio);

    // ── Collisions ──────────────────────────────────────────────────────
    private void CheckCollisions()
    {
        // Temporarily ignore colliders the player is standing inside
        Physics2D.queriesStartInColliders = false;

        bool groundHit = Physics2D.CapsuleCast(
            _col.bounds.center,
            _col.size,
            _col.direction,
            0f,
            Vector2.down,
            grounderDistance,
            groundLayer);

        bool ceilingHit = Physics2D.CapsuleCast(
            _col.bounds.center,
            _col.size,
            _col.direction,
            0f,
            Vector2.up,
            grounderDistance,
            groundLayer);

        // if player bonks head on ceiling just kill upward velocity
        if (ceilingHit)
            _frameVelocity.y = Mathf.Min(0f, _frameVelocity.y);

        // Just landed
        if (!_grounded && groundHit)
        {
            _grounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
        }
        // Just left the ground
        else if (_grounded && !groundHit)
        {
            _grounded = false;
            _timeLeftGround = _time;
        }

        // ── Wall Detection ─────────────────────────────

        bool wallLeft = Physics2D.CapsuleCast(
            _col.bounds.center,
            _col.size,
            _col.direction,
            0f,
            Vector2.left,
            wallCheckDistance,
            groundLayer);

        bool wallRight = Physics2D.CapsuleCast(
            _col.bounds.center,
            _col.size,
            _col.direction,
            0f,
            Vector2.right,
            wallCheckDistance,
            groundLayer);

        _onWall = !_grounded &&
          _frameVelocity.y < 0f &&
          (wallLeft || wallRight);

        if (wallLeft)
            _wallDirection = -1;
        else if (wallRight)
            _wallDirection = 1;

        Physics2D.queriesStartInColliders = true;
    }
    // ── Jump ────────────────────────────────────────────────────────────
    private void HandleJump()
    {
        // Detect early jump release
        if (!_endedJumpEarly && !_grounded && !_jumpHeld && _frameVelocity.y > 0f)
            _endedJumpEarly = true;

        if (!_jumpToConsume && !HasBufferedJump) return;

        if (_onWall)
        {
            WallJump();
        }
        else if (_grounded || CanUseCoyote)
        {
            Jump();        
        }

        _jumpToConsume = false;
    }

    private void Jump()
    {
        _endedJumpEarly     = false;
        _timeJumpWasPressed = 0f;
        _bufferedJumpUsable = false;
        _coyoteUsable       = false;
        _frameVelocity.y    = jumpPower;
    }

    private void WallJump()
    {
        _endedJumpEarly = false;

        _frameVelocity.x = -_wallDirection * wallJumpHorizontalForce;
        _frameVelocity.y = wallJumpVerticalForce;
    }

    // ── Horizontal movement ─────────────────────────────────────────────
    private void HandleHorizontal()
    {
        float moveX = _moveInput.x;

        // Track which way the player is facing, used by the Builder throw.
        if (moveX > 0f) _facingDirection = 1;
        else if (moveX < 0f) _facingDirection = -1;

        // if on a wall, ignore movement pushing into the wall
        if (_onWall)
        {
            if ((_wallDirection == 1 && moveX > 0f) ||   // right wall + holding right
                (_wallDirection == -1 && moveX < 0f))    // left wall + holding left
            {
                moveX = 0f;
            }
        }

        float targetSpeed = maxSpeed;

        if (_sprinting)
            targetSpeed *= sprintMultiplier;

        if (moveX == 0f)
        {
            float decel = _grounded ? groundDeceleration : airDeceleration;

            _frameVelocity.x = Mathf.MoveTowards(
                _frameVelocity.x,
                0f,
                decel * Time.fixedDeltaTime);
        }
        else
        {
            _frameVelocity.x = Mathf.MoveTowards(
                _frameVelocity.x,
                moveX * targetSpeed,
                acceleration * Time.fixedDeltaTime);
        }
    }
    public void OnSprint(InputValue value)
    {
        _sprinting = value.isPressed;
    }

    // ── Gravity ─────────────────────────────────────────────────────────
    private void HandleGravity()
    {
        if (_onWall)
        {
            _frameVelocity.y = Mathf.Max(
                _frameVelocity.y - fallAcceleration * 0.5f * Time.fixedDeltaTime,
                -wallSlideSpeed);

            return;
        }

        if (_grounded && _frameVelocity.y <= 0f)
        {
            // small constant downward force keeps the player stuck to slopes
            _frameVelocity.y = groundingForce;
        }
        else
        {
            float gravity = fallAcceleration;

            // we multiply gravity when jump was released earlier than normal to make it a smaller jump 
            if (_endedJumpEarly && _frameVelocity.y > 0f)
                gravity *= jumpEndEarlyGravityModifier;

            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y,
                -maxFallSpeed, gravity * Time.fixedDeltaTime);
        }
    }

    // ── Apply ────────────────────────────────────────────────────────────
    private void ApplyMovement()
    {
        _rb.linearVelocity = _frameVelocity;
    }

    // ── Freeze Inputs ───────────────────────────────────────────────────────────
    public void SetMovementLocked(bool locked)
    {
        // In real co-op, movement never locks — each player always controls
        // their own character. Locking is a solo-only
        if (GameManager.IsMultiplayer)
            locked = false;

        Debug.Log($"Movement Locked: {locked}");
        _movementLocked = locked;

        if (locked)
        {
            _moveInput = Vector2.zero;

            _jumpToConsume = false;
            _bufferedJumpUsable = false;
            _jumpHeld = false;
        }
    }
}