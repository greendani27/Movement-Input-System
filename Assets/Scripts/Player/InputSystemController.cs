using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(Animator))]
public class InputSystemController : MonoBehaviour
{
    // ── Estado interno ───────────────────────────────────────────────────────────
    private Rigidbody rb;
    private bool isGrounded;
    private float jumpCounter;
    private float coyoteTimeCounter;
    private float speed;
    private bool isRunning;
    private Vector2 moveInput;
    private Transform cameraTransform;

    // ── Parámetros serializados ──────────────────────────────────────────────────
    [SerializeField] private float coyoteTime;
    [SerializeField] private float runSpeed;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float jumpheight;
    [SerializeField] private float rotationSpeed = 10f;

    // ── Input System ─────────────────────────────────────────────────────────────
    [SerializeField] private InputActionAsset playerInput;
    private InputActionMap playerMap;
    private InputAction move;
    private InputAction jump;
    private InputAction look;
    private InputAction run;
    private InputAction crouch;
    private InputAction grab;

    // ── Componentes ──────────────────────────────────────────────────────────────
    [SerializeField] private Animator animator;

    // ── Push Block ───────────────────────────────────────────────────────────────
    [SerializeField] private float grabCheckDistance = 1.2f;      // Distancia máxima para detectar el bloque
    [SerializeField] private LayerMask pushableLayer;             // Layer "Pushable" en el Inspector

    private PushableBlock grabbedBlock = null;
    private bool isGrabbing = false;


    // ════════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jumpCounter = 0;
        cameraTransform = Camera.main.transform;
        speed = walkSpeed;

        playerMap = playerInput.FindActionMap("Player");

        move = playerMap.FindAction("Move");
        jump = playerMap.FindAction("Jump");
        look = playerMap.FindAction("Look");
        run = playerMap.FindAction("Run");
        crouch = playerMap.FindAction("Crouch");
        grab = playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        jump.performed += OnJumpPerformed;
        crouch.started += OnCrouchPerformed;
        crouch.canceled += OnCrouchPerformed;
        run.started += OnRunPerformed;
        run.canceled += OnRunPerformed;
        grab.started += OnGrabPerformed;
        grab.canceled += OnGrabPerformed;
    }

    private void OnDisable()
    {
        jump.performed -= OnJumpPerformed;
        crouch.started -= OnCrouchPerformed;
        crouch.canceled -= OnCrouchPerformed;
        run.started -= OnRunPerformed;
        run.canceled -= OnRunPerformed;
        grab.started -= OnGrabPerformed;
        grab.canceled -= OnGrabPerformed;
    }

    void Update()
    {
        moveInput = move.ReadValue<Vector2>();
        UpdateAnimatorSpeed();
    }

    private void FixedUpdate()
    {
        if (isGrabbing)
        {
            HandleBlockPush();
            return; // El jugador no se mueve libremente
        }

        if (moveInput.sqrMagnitude < 0.01f)
        {
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


    // ════════════════════════════════════════════════════════════════════════════
    // Input Callbacks
    // ════════════════════════════════════════════════════════════════════════════

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
    public void OnGrabPerformed(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            TryGrabBlock();
        }
        if (context.canceled)
        {
            ReleaseBlock();
        }
    }


    // ════════════════════════════════════════════════════════════════════════════
    // Movimiento
    // ════════════════════════════════════════════════════════════════════════════

    private void UpdateAnimatorSpeed()
    {
        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        if (!isMoving)
        {
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", speed);
        }
    }


    // ════════════════════════════════════════════════════════════════════════════
    // Push Block
    // ════════════════════════════════════════════════════════════════════════════

    private void HandleBlockPush()
    {
        if (grabbedBlock == null) return;

        // Proyectamos el input sobre el eje permitido del bloque
        // para que solo el movimiento "hacia adelante/atrás" cuente
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        float projected = Vector3.Dot(inputDir, grabbedBlock.transform.position - transform.position);

        grabbedBlock.SetInput(Mathf.Clamp(projected, -1f, 1f));

        // El jugador se mueve igual que el bloque pero con su propio Rigidbody
        rb.linearVelocity = grabbedBlock.GetComponent<Rigidbody>().linearVelocity;
    }

    private void TryGrabBlock()
    {
        // Lanzamos un ray desde el jugador hacia donde mira
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, grabCheckDistance, pushableLayer))
        {
            PushableBlock block = hit.collider.GetComponent<PushableBlock>();
            if (block == null) return;

            grabbedBlock = block;
            isGrabbing = true;

            // Calculamos el eje permitido: el que va del jugador al bloque (X o Z)
            Vector3 toBlock = (hit.collider.transform.position - transform.position).normalized;
            Vector3 axis = Mathf.Abs(toBlock.x) > Mathf.Abs(toBlock.z)
                              ? new Vector3(Mathf.Sign(toBlock.x), 0, 0)
                              : new Vector3(0, 0, Mathf.Sign(toBlock.z));

            block.StartPush(axis);
            animator.SetBool("Push", true);
        }
    }

    private void ReleaseBlock()
    {
        if (grabbedBlock != null)
        {
            grabbedBlock.StopPush();
            grabbedBlock = null;
        }
        isGrabbing = false;
        animator.SetBool("Push", false);
    }
}
