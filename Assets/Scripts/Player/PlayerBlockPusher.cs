using UnityEngine;

/// <summary>
/// Gestiona el agarre, empuje y suelta de bloques PushableObject.
/// Se comunica con PlayerMovement para mover al jugador en sincronía con el bloque.
/// </summary>
public class PlayerBlockPusher : MonoBehaviour
{
    // ── Parámetros ───────────────────────────────────────────────────────────
    [SerializeField] private float     grabCheckDistance = 1.2f;
    [SerializeField] private LayerMask pushableLayer;

    // ── Estado interno ───────────────────────────────────────────────────────
    private PushableObject grabbedBlock = null;

    // ── Dependencias ─────────────────────────────────────────────────────────
    private PlayerMovement          movement;
    private PlayerAnimatorController anim;
    private PlayerStateController   stateCtrl;
    private Transform               cameraTransform;

    // ── Propiedad pública ────────────────────────────────────────────────────
    public bool IsGrabbing => stateCtrl.Is(PlayerState.Grabbing);

    private void Awake()
    {
        movement        = GetComponent<PlayerMovement>();
        anim            = GetComponent<PlayerAnimatorController>();
        stateCtrl       = GetComponent<PlayerStateController>();
        cameraTransform = Camera.main.transform;
    }

    // ════════════════════════════════════════════════════════════════════════
    // API pública — llamada desde el orquestador
    // ════════════════════════════════════════════════════════════════════════

    public void TryGrab()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, grabCheckDistance, pushableLayer))
            return;

        PushableObject block = hit.collider.GetComponent<PushableObject>();
        if (block == null) return;

        // Alinear el jugador con la cara del bloque
        Vector3 normal   = hit.normal;
        normal.y = 0f;
        normal.Normalize();

        Vector3 blockPos  = hit.collider.transform.position;
        Vector3 playerPos = transform.position;

        Vector3 snapped = Mathf.Abs(normal.x) > 0.5f
            ? new Vector3(playerPos.x, playerPos.y, blockPos.z)
            : new Vector3(blockPos.x,  playerPos.y, playerPos.z);

        transform.position = snapped;

        Vector3 toBlock = blockPos - transform.position;
        toBlock.y = 0f;
        if (toBlock.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(toBlock.normalized);

        // Guardar referencia y cambiar estado
        grabbedBlock = block;
        stateCtrl.SetState(PlayerState.Grabbing);
        movement.SetSpeed(movement.WalkSpeed);

        anim.SetPush(true);
        anim.SetSpeed(0f);
        anim.SetCrouch(false);
    }

    /// <summary>
    /// Maneja el movimiento del bloque y del jugador mientras está en estado Grabbing.
    /// Debe llamarse desde FixedUpdate del orquestador.
    /// </summary>
    public void HandlePush(Vector2 moveInput)
    {
        if (grabbedBlock == null) { Release(); return; }

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Liberamos rotación del bloque para que pueda moverse
        grabbedBlock.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;

        Vector3 inputDir = camForward * moveInput.y + camRight * moveInput.x;

        // Restringir a direcciones cardinales
        Vector3 cardinalDir = Vector3.zero;
        if (inputDir.sqrMagnitude > 0.01f)
        {
            cardinalDir = Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.z)
                ? new Vector3(Mathf.Sign(inputDir.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(inputDir.z));
        }

        Vector3 desiredVelocity = cardinalDir * movement.WalkSpeed;

        grabbedBlock.SetVelocity(desiredVelocity);
        movement.MoveWithVelocity(desiredVelocity);
        movement.RotateTowards(grabbedBlock.transform.position);

        anim.SetSpeed(desiredVelocity.sqrMagnitude > 0.01f ? 1f : 0f);
    }

    public void Release()
    {
        if (grabbedBlock != null)
        {
            grabbedBlock.Stop();
            grabbedBlock = null;
        }

        stateCtrl.SetState(PlayerState.Idle);
        anim.SetPush(false);
    }
}
