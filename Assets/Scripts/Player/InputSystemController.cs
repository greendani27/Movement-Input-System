using UnityEngine;

/// <summary>
/// Orquestador del jugador.
/// No contiene lógica propia: suscribe eventos del input y delega en los
/// componentes especializados. Añade [RequireComponent] para garantizar
/// que todos los scripts hermanos están presentes en el GameObject.
/// </summary>
[RequireComponent(typeof(PlayerStateController))]
[RequireComponent(typeof(PlayerInputHandler))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAnimatorController))]
[RequireComponent(typeof(PlayerInteraction))]
[RequireComponent(typeof(PlayerBlockPusher))]
public class InputSystemController : MonoBehaviour
{
    // ── Dependencias ─────────────────────────────────────────────────────────
    private PlayerStateController    stateCtrl;
    private PlayerInputHandler       input;
    private PlayerMovement           movement;
    private PlayerAnimatorController anim;
    private PlayerBlockPusher        blockPusher;

    private Transform cameraTransform;

    // ════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        stateCtrl   = GetComponent<PlayerStateController>();
        input       = GetComponent<PlayerInputHandler>();
        movement    = GetComponent<PlayerMovement>();
        anim        = GetComponent<PlayerAnimatorController>();
        blockPusher = GetComponent<PlayerBlockPusher>();

        cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        input.OnJump   += HandleJump;
        input.OnCrouch += HandleCrouch;
        input.OnRun    += HandleRun;
        input.OnGrab   += HandleGrab;
    }

    private void OnDisable()
    {
        input.OnJump   -= HandleJump;
        input.OnCrouch -= HandleCrouch;
        input.OnRun    -= HandleRun;
        input.OnGrab   -= HandleGrab;
    }

    private void Update()
    {
        if (!stateCtrl.Is(PlayerState.Grabbing))
            UpdateAnimatorSpeed();
    }

    private void FixedUpdate()
    {
        if (stateCtrl.Is(PlayerState.Grabbing))
        {
            blockPusher.HandlePush(input.MoveInput);
            return;
        }

        Vector3 moveDirection = GetCameraRelativeDirection(input.MoveInput);
        movement.Move(moveDirection);
    }

    // ════════════════════════════════════════════════════════════════════════
    // Handlers de input
    // ════════════════════════════════════════════════════════════════════════

    private void HandleJump()
    {
        if (stateCtrl.Is(PlayerState.Grabbing)) return;
        movement.TryJump();
    }

    private void HandleCrouch(bool started)
    {
        if (stateCtrl.Is(PlayerState.Grabbing)) return;
        anim.SetCrouch(started);
    }

    private void HandleRun(bool started)
    {
        if (stateCtrl.Is(PlayerState.Grabbing)) return;

        movement.SetRunning(started);
        stateCtrl.SetState(started ? PlayerState.Running : PlayerState.Idle);
    }

    private void HandleGrab(bool started)
    {
        if (started) blockPusher.TryGrab();
        else         blockPusher.Release();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════════

    private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return camForward * moveInput.y + camRight * moveInput.x;
    }

    private void UpdateAnimatorSpeed()
    {
        bool isMoving = input.MoveInput.sqrMagnitude > 0.01f;
        anim.SetSpeed(isMoving ? movement.CurrentSpeed : 0f);
    }
}
