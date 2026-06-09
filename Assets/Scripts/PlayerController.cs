using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerController
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float acceleration = 80f;
        [SerializeField] private float groundDeceleration = 70f;
        [SerializeField] private float airDeceleration = 30f;

        [Header("Jump")]
        [SerializeField] private float jumpPower = 18f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBuffer = 0.12f;

        [Header("Gravity")]
        [SerializeField] private float fallAcceleration = 65f;
        [SerializeField] private float maxFallSpeed = 30f;
        [SerializeField] private float jumpEndEarlyGravityModifier = 3f;
        [SerializeField] private float groundingForce = -1.5f;

        [Header("Collision")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float grounderDistance = 0.05f;

        // Components
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;

        // State
        private Vector2 _frameVelocity;
        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;

        // Jump internals
        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;
        private float _timeLeftGround;
        private bool _grounded;
        private float _time;

        // Helpers
        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + jumpBuffer;
        private bool CanUseCoyote   => _coyoteUsable && !_grounded && _time < _timeLeftGround + coyoteTime;

        // ── Public read-only state ──────────────────────────────────────────
        public bool IsGrounded => _grounded;
        public Vector2 Velocity => _rb.linearVelocity;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _rb.gravityScale = 0f; //we aint using unity default gravity, so disabled
        }

        // ── Unity Input System ────────────────────────────────────────

        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                _jumpPressed    = true;
                _jumpToConsume  = true;
                _timeJumpWasPressed = _time;
            }
            _jumpHeld = value.isPressed;
        }

        public void OnJumpCanceled(InputValue value)
        {
            _jumpHeld = false;
        }

        // ── Main loop ───────────────────────────────────────────────────────
        private void Update()
        {
            _time += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            HandleJump();
            HandleHorizontal();
            HandleGravity();
            ApplyMovement();

            _jumpPressed = false; // consume press flag each physics step
        }

        // ── Collisions ──────────────────────────────────────────────────────
        private void CheckCollisions()
        {
            // Temporarily ignore colliders the player is standing inside
            Physics2D.queriesStartInColliders = false;

            bool groundHit  = Physics2D.CapsuleCast(_col.bounds.center, _col.size,
                                  _col.direction, 0f, Vector2.down, grounderDistance, groundLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size,
                                  _col.direction, 0f, Vector2.up,   grounderDistance, groundLayer);

            // if player bonks head on ceiling just kill upward velocity
            if (ceilingHit)
                _frameVelocity.y = Mathf.Min(0f, _frameVelocity.y);

            // Just landed
            if (!_grounded && groundHit)
            {
                _grounded            = true;
                _coyoteUsable        = true;
                _bufferedJumpUsable  = true;
                _endedJumpEarly      = false;
            }
            // Just left the ground
            else if (_grounded && !groundHit)
            {
                _grounded       = false;
                _timeLeftGround = _time;
            }

            Physics2D.queriesStartInColliders = true;
        }

        // ── Jump ────────────────────────────────────────────────────────────
        private void HandleJump()
        {
            // Detect early jump release
            if (!_endedJumpEarly && !_grounded && !_jumpHeld && _frameVelocity.y > 0f)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote)
                Jump();

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

        // ── Horizontal movement ─────────────────────────────────────────────
        private void HandleHorizontal()
        {
            if (_moveInput.x == 0f)
            {
                // Decelerate to a stop
                float decel = _grounded ? groundDeceleration : airDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0f, decel * Time.fixedDeltaTime);
            }
            else
            {
                // Accelerate toward max speed
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x,
                    _moveInput.x * maxSpeed, acceleration * Time.fixedDeltaTime);
            }
        }

        // ── Gravity ─────────────────────────────────────────────────────────
        private void HandleGravity()
        {
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
    }
}