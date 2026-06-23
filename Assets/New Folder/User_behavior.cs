using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class User_behavior : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float groundStickForce = 3f;

    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool isRunning;
    private bool jumpQueued;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = 0f;
            isRunning = false;
            return;
        }

        bool moveLeft = keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
        bool moveRight = keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;

        moveInput = 0f;
        if (moveLeft)
        {
            moveInput -= 1f;
        }

        if (moveRight)
        {
            moveInput += 1f;
        }

        isRunning = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        if (keyboard.spaceKey.wasPressedThisFrame && TryGetGround(out _))
        {
            jumpQueued = true;
        }

        if (spriteRenderer != null && moveInput != 0f)
        {
            spriteRenderer.flipX = moveInput < 0f;
        }
    }

    private void FixedUpdate()
    {
        float targetSpeed = isRunning ? runSpeed : walkSpeed;
        bool isGrounded = TryGetGround(out Vector2 groundNormal);
        bool wantsToMove = Mathf.Abs(moveInput) > 0.01f;

        if (isGrounded && !jumpQueued)
        {
            if (wantsToMove)
            {
                Vector2 slopeDirection = new Vector2(groundNormal.y, -groundNormal.x);
                rb.linearVelocity = slopeDirection * (moveInput * targetSpeed);
                rb.AddForce(-groundNormal * groundStickForce, ForceMode2D.Force);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput * targetSpeed, rb.linearVelocity.y);
        }

        if (!jumpQueued)
        {
            return;
        }

        if (isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        jumpQueued = false;
    }

    private bool TryGetGround(out Vector2 groundNormal)
    {
        groundNormal = Vector2.up;

        ContactFilter2D groundFilter = new ContactFilter2D
        {
            useTriggers = false
        };

        int hitCount = bodyCollider.Cast(Vector2.down, groundFilter, groundHits, groundCheckDistance);
        bool foundGround = false;
        float bestNormalY = 0f;

        for (int i = 0; i < hitCount; i++)
        {
            if (groundHits[i].normal.y > 0.5f)
            {
                if (!foundGround || groundHits[i].normal.y > bestNormalY)
                {
                    groundNormal = groundHits[i].normal;
                    bestNormalY = groundNormal.y;
                    foundGround = true;
                }
            }
        }

        return foundGround;
    }
}
