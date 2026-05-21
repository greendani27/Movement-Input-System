using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemController : MonoBehaviour
{
    private Rigidbody rb;
    private bool isGrounded;
    private float jumpCounter;
    private float coyoteTimeCounter;
    private float speed;
    private bool isRunning;

    public float coyoteTime;
    public float runSpeed;
    public float walkSpeed;
    public float jumpheight;
    public float rotationSpeed = 10f;

    private Vector2 moveInput;
    private Transform cameraTransform;

    public InputActionReference move;
    public InputActionReference jump;
    public InputActionReference look;
    public InputActionReference run; // ← Asigna tu acción de Shift/L3 aquí

    [SerializeField] Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jumpCounter = 0;
        cameraTransform = Camera.main.transform;
        speed = walkSpeed;
    }

    void Update()
    {
        moveInput = move.action.ReadValue<Vector2>();
        UpdateAnimatorSpeed();
    }

    private void UpdateAnimatorSpeed()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            animator.SetFloat("Speed", 0f);        // Idle
        }
        else if (isRunning)
        {
            animator.SetFloat("Speed", 2f);        // Run
        }
        else
        {
            animator.SetFloat("Speed", 1f);        // Walk
        }
    }

    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude < 0.01f)
        {
            // Detener movimiento horizontal sin afectar la gravedad
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;

        rb.linearVelocity = new Vector3(
            moveDirection.x * speed,
            rb.linearVelocity.y,
            moveDirection.z * speed
        );

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    private void OnEnable()
    {
        jump.action.performed += OnJumpPerformed;
        run.action.started += OnRunPerformed;
        run.action.canceled += OnRunPerformed;
    }

    private void OnDisable()
    {
        jump.action.performed -= OnJumpPerformed;
        run.action.started -= OnRunPerformed;
        run.action.canceled -= OnRunPerformed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
        coyoteTimeCounter = coyoteTime;
        jumpCounter = 0;
        animator.SetBool("Jump", false);
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        coyoteTimeCounter -= Time.deltaTime;
    }

    public void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if ((isGrounded || jumpCounter < 2) && coyoteTimeCounter > 0)
        {
            rb.AddForce(Vector3.up * jumpheight, ForceMode.Impulse);
            jumpCounter += 1;
            animator.SetBool("Jump", true);
        }
    }

    public void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        if (context.started) animator.SetBool("Crouch", true);
        if (context.canceled) animator.SetBool("Crouch", false);
    }

    public void OnRunPerformed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isRunning = true;
            speed = runSpeed;
        }
        if (context.canceled)
        {
            isRunning = false;
            speed = walkSpeed;
        }
    }
}