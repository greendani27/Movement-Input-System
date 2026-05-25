using UnityEngine;

/// <summary>
/// Gestiona toda la física del jugador: movimiento, rotación, salto y coyote time.
/// Recibe órdenes del orquestador; no lee input directamente.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    // ── Parámetros ───────────────────────────────────────────────────────────
    [SerializeField] private float walkSpeed     = 5f;
    [SerializeField] private float runSpeed      = 9f;
    [SerializeField] private float jumpHeight    = 7f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float coyoteTime    = 0.15f;

    // ── Propiedades públicas de solo lectura ─────────────────────────────────
    public float WalkSpeed => walkSpeed;
    public float RunSpeed  => runSpeed;
    public float CurrentSpeed { get; private set; }

    // ── Estado interno ───────────────────────────────────────────────────────
    private Rigidbody rb;
    private bool  isGrounded;
    private float jumpCounter;
    private float coyoteTimeCounter;

    // ── Dependencias ─────────────────────────────────────────────────────────
    private PlayerAnimatorController anim;

    private void Awake()
    {
        rb   = GetComponent<Rigidbody>();
        anim = GetComponent<PlayerAnimatorController>();

        CurrentSpeed = walkSpeed;
    }

    // ════════════════════════════════════════════════════════════════════════
    // API pública
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Mueve el jugador en la dirección dada a la velocidad actual.</summary>
    public void Move(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        rb.linearVelocity = new Vector3(
            direction.x * CurrentSpeed,
            rb.linearVelocity.y,
            direction.z * CurrentSpeed
        );

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation,
                        rotationSpeed * Time.fixedDeltaTime));
    }

    /// <summary>Mueve el jugador con una velocidad explícita (usado al empujar bloques).</summary>
    public void MoveWithVelocity(Vector3 velocity)
    {
        rb.linearVelocity = new Vector3(velocity.x, rb.linearVelocity.y, velocity.z);
    }

    /// <summary>Rota el jugador hacia un punto del mundo.</summary>
    public void RotateTowards(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot,
                        rotationSpeed * Time.fixedDeltaTime));
    }

    /// <summary>Intenta ejecutar un salto si las condiciones lo permiten.</summary>
    public void TryJump()
    {
        if ((isGrounded || jumpCounter < 2) && coyoteTimeCounter > 0)
        {
            rb.AddForce(Vector3.up * jumpHeight, ForceMode.Impulse);
            jumpCounter += 1;
            anim.SetJump(true);
        }
    }

    public void SetRunning(bool running)
    {
        CurrentSpeed = running ? runSpeed : walkSpeed;
    }

    public void SetSpeed(float speed)
    {
        CurrentSpeed = speed;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Colisiones (suelo)
    // ════════════════════════════════════════════════════════════════════════

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded          = true;
        coyoteTimeCounter   = coyoteTime;
        jumpCounter         = 0;
        anim.SetJump(false);
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded          = false;
        coyoteTimeCounter  -= Time.deltaTime;
    }
}
