using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerState { Idle, Walking, Running, Jumping, Grabbing }

[RequireComponent(typeof(Collider), typeof(Rigidbody), typeof(Animator))]
public class InputSystemController : MonoBehaviour
{
    // ── Estado interno ───────────────────────────────────────────────────────────
    private Rigidbody rb;
    private PlayerState currentState = PlayerState.Idle;
    private bool isGrounded;
    private float jumpCounter;
    private float coyoteTimeCounter;
    private float speed;
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
    [SerializeField] private float grabCheckDistance = 1.2f;
    [SerializeField] private LayerMask pushableLayer;

    private PushableObject grabbedBlock = null;
    private bool isGrabbing = false;

    // ── Interact Highlight ────────────────────────────────────────────────────────
    [SerializeField] private float interactCheckDistance = 2f;
    [SerializeField] private GameObject interactUI;
    private IInteractable currentLookedAt = null;


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

    private void Update()
    {
        moveInput = move.ReadValue<Vector2>();
        UpdateAnimatorSpeed();
        CheckInteractableLook();
    }

    private void FixedUpdate()
    {
        if (currentState == PlayerState.Grabbing)
        {
            HandleBlockPush();
            return;
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
        if (currentState == PlayerState.Grabbing) return;

        if ((isGrounded || jumpCounter < 2) && coyoteTimeCounter > 0)
        {
            rb.AddForce(Vector3.up * jumpheight, ForceMode.Impulse);
            jumpCounter += 1;
            animator.SetBool("Jump", true);
        }
    }

    public void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        if (currentState == PlayerState.Grabbing) return;

        if (context.started) animator.SetBool("Crouch", true);
        if (context.canceled) animator.SetBool("Crouch", false);
    }

    public void OnRunPerformed(InputAction.CallbackContext context)
    {
        if (currentState == PlayerState.Grabbing) return;

        if (context.started)
        {
            currentState = PlayerState.Running;
            speed = runSpeed;
        }
        if (context.canceled)
        {
            currentState = PlayerState.Idle;
            speed = walkSpeed;
        }
    }

    public void OnGrabPerformed(InputAction.CallbackContext context)
    {
        if (context.started) TryGrabBlock();
        if (context.canceled) ReleaseBlock();
    }


    // ════════════════════════════════════════════════════════════════════════════
    // Movimiento
    // ════════════════════════════════════════════════════════════════════════════

    private void UpdateAnimatorSpeed()
    {
        if (currentState == PlayerState.Grabbing) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetFloat("Speed", isMoving ? speed : 0f);
    }


    // ════════════════════════════════════════════════════════════════════════════
    // Interact Highlight
    // ════════════════════════════════════════════════════════════════════════════

    private void CheckInteractableLook()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactCheckDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentLookedAt != interactable)
                {
                    currentLookedAt = interactable;
                    interactUI.SetActive(true);
                }
                return;
            }
        }

        if (currentLookedAt != null)
        {
            currentLookedAt = null;
            interactUI.SetActive(false);
        }
    }


    // ════════════════════════════════════════════════════════════════════════════
    // Push Block
    // ════════════════════════════════════════════════════════════════════════════

    private void TryGrabBlock()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, grabCheckDistance, pushableLayer))
            return;

        PushableObject block = hit.collider.GetComponent<PushableObject>();
        if (block == null) return;

        Vector3 normal = hit.normal;
        normal.y = 0f;
        normal.Normalize();

        Vector3 blockPos = hit.collider.transform.position;
        Vector3 playerPos = transform.position;

        Vector3 snapped = Mathf.Abs(normal.x) > 0.5f
            ? new Vector3(playerPos.x, playerPos.y, blockPos.z)
            : new Vector3(blockPos.x, playerPos.y, playerPos.z);

        transform.position = snapped;

        Vector3 toBlock = blockPos - transform.position;
        toBlock.y = 0f;
        if (toBlock.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toBlock.normalized);

        grabbedBlock = block;
        currentState = PlayerState.Grabbing;
        speed = walkSpeed;

        animator.SetBool("Push", true);
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Crouch", false);
    }

    private void HandleBlockPush()
    {
        if (grabbedBlock == null) { ReleaseBlock(); return; }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        grabbedBlock.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;

        Vector3 cardinalDir = Vector3.zero;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.z))
                cardinalDir = new Vector3(Mathf.Sign(inputDir.x), 0f, 0f);
            else
                cardinalDir = new Vector3(0f, 0f, Mathf.Sign(inputDir.z));
        }

        Vector3 desiredVelocity = cardinalDir * walkSpeed;

        grabbedBlock.SetVelocity(desiredVelocity);

        rb.linearVelocity = new Vector3(
            desiredVelocity.x,
            rb.linearVelocity.y,
            desiredVelocity.z
        );

        Vector3 toBlock = grabbedBlock.transform.position - transform.position;
        toBlock.y = 0f;
        if (toBlock.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(toBlock.normalized);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot,
                            rotationSpeed * Time.fixedDeltaTime));
        }

        animator.SetFloat("Speed", desiredVelocity.sqrMagnitude > 0.01f ? 1f : 0f);
    }

    private void ReleaseBlock()
    {
        if (grabbedBlock != null)
        {
            grabbedBlock.Stop();
            grabbedBlock = null;
        }

        currentState = PlayerState.Idle;
        animator.SetBool("Push", false);
    }
}