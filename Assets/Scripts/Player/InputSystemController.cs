using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent (typeof(Collider), typeof(Rigidbody), typeof(Animator))]
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
    public InputActionReference run;

    [SerializeField] Animator animator;

    [Header("Push Block")]
    public float pushCheckDistance = 0.75f;
    public LayerMask pushableLayer;

    private PushableObject grabbedBlock = null;
    private bool isGrabbing = false;
    private Vector3 pushAxis;
    private Vector3 snapPosition;

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
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", speed);
        }

    }

    private void FixedUpdate()
    {
        if (isGrabbing)
        {
            HandleBlockPush();
            return;
        }

        // ── Movimiento normal ────────────────────────────────────────────────────
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
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                        rotationSpeed * Time.fixedDeltaTime));

        // ── Detección de bloque al frente ────────────────────────────────────────
        CheckForBlock(moveDirection);
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

    private void CheckForBlock(Vector3 moveDirection)
    {
        // Solo detectamos si el jugador se mueve hacia el bloque
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, moveDirection.normalized);

        if (!Physics.Raycast(ray, out RaycastHit hit, pushCheckDistance, pushableLayer))
            return;

        PushableObject block = hit.collider.GetComponent<PushableObject>();
        if (block == null) return;

        // ── Calcular el eje permitido (X o Z según la cara del bloque tocada) ───
        Vector3 normal = hit.normal; // Normal de la cara que tocamos
        normal.y = 0f;

        // El eje de empuje es el opuesto a la normal de la cara
        // Ej: si la normal es (1,0,0) → empujamos en (-1,0,0)
        pushAxis = -normal.normalized;
        pushAxis = Mathf.Abs(pushAxis.x) > Mathf.Abs(pushAxis.z)
                    ? new Vector3(Mathf.Sign(pushAxis.x), 0f, 0f)
                    : new Vector3(0f, 0f, Mathf.Sign(pushAxis.z));

        // ── Alinear al jugador con el centro del bloque en el eje perpendicular ─
        // (igual que OoT: el jugador se centra automáticamente)
        Vector3 blockPos = hit.collider.transform.position;
        Vector3 playerPos = transform.position;

        if (Mathf.Abs(pushAxis.x) > 0.5f) // Empujamos en X → alineamos en Z
            snapPosition = new Vector3(playerPos.x, playerPos.y, blockPos.z);
        else                               // Empujamos en Z → alineamos en X
            snapPosition = new Vector3(blockPos.x, playerPos.y, playerPos.z);

        StartGrab(block);
    }

    private void StartGrab(PushableObject block)
    {
        grabbedBlock = block;
        isGrabbing = true;

        // Alineamos al jugador instantáneamente al centro del bloque
        Vector3 pos = transform.position;
        transform.position = new Vector3(snapPosition.x, pos.y, snapPosition.z);

        block.StartPush(pushAxis);
        animator.SetBool("Push", true);
        animator.SetFloat("Speed", 0f);
    }

    private void HandleBlockPush()
    {
        if (grabbedBlock == null) { ReleaseBlock(); return; }

        // ── Proyectar el input sobre el eje de empuje ────────────────────────────
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f; camRight.y = 0f;
        camForward.Normalize(); camRight.Normalize();

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;
        float projected = Vector3.Dot(inputDir, pushAxis);

        // ── Si el jugador empuja hacia atrás (se aleja), soltamos ───────────────
        if (projected < -0.3f)
        {
            ReleaseBlock();
            return;
        }

        float inputMag = Mathf.Clamp01(projected); // Solo positivo: solo empuja, no jala
        grabbedBlock.SetInput(inputMag);

        // ── El jugador sigue al bloque ───────────────────────────────────────────
        Vector3 blockVel = grabbedBlock.GetVelocity();
        rb.linearVelocity = new Vector3(blockVel.x, rb.linearVelocity.y, blockVel.z);

        // ── Mantener la rotación encarada al bloque ──────────────────────────────
        if (grabbedBlock != null)
        {
            Vector3 toBlock = (grabbedBlock.transform.position - transform.position);
            toBlock.y = 0f;
            if (toBlock.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toBlock.normalized);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot,
                                rotationSpeed * Time.fixedDeltaTime));
            }
        }

        // ── Animación: push activo solo si el bloque se mueve ───────────────────
        animator.SetFloat("Speed", inputMag > 0.1f ? 1f : 0f);
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