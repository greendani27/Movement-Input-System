using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Único punto de contacto con el Input System.
/// Lee acciones y expone valores + eventos; no mueve nada.
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset playerInput;

    // ── Propiedades públicas de lectura ──────────────────────────────────────
    public Vector2 MoveInput  { get; private set; }

    // ── Eventos ──────────────────────────────────────────────────────────────
    public event System.Action            OnJump;
    public event System.Action<bool>      OnCrouch;   // true = started, false = canceled
    public event System.Action<bool>      OnRun;      // true = started, false = canceled
    public event System.Action<bool>      OnGrab;     // true = started, false = canceled

    // ── Acciones privadas ────────────────────────────────────────────────────
    private InputActionMap playerMap;
    private InputAction move;
    private InputAction jump;
    private InputAction run;
    private InputAction crouch;
    private InputAction grab;

    private void Awake()
    {
        playerMap = playerInput.FindActionMap("Player");

        move   = playerMap.FindAction("Move");
        jump   = playerMap.FindAction("Jump");
        run    = playerMap.FindAction("Run");
        crouch = playerMap.FindAction("Crouch");
        grab   = playerMap.FindAction("Interact");
    }

    private void OnEnable()
    {
        playerMap.Enable();

        jump.performed   += OnJumpPerformed;
        crouch.started   += OnCrouchStarted;
        crouch.canceled  += OnCrouchCanceled;
        run.started      += OnRunStarted;
        run.canceled     += OnRunCanceled;
        grab.started     += OnGrabStarted;
        grab.canceled    += OnGrabCanceled;
    }

    private void OnDisable()
    {
        jump.performed   -= OnJumpPerformed;
        crouch.started   -= OnCrouchStarted;
        crouch.canceled  -= OnCrouchCanceled;
        run.started      -= OnRunStarted;
        run.canceled     -= OnRunCanceled;
        grab.started     -= OnGrabStarted;
        grab.canceled    -= OnGrabCanceled;

        playerMap.Disable();
    }

    private void Update()
    {
        MoveInput = move.ReadValue<Vector2>();
    }

    // ── Callbacks internos ───────────────────────────────────────────────────
    private void OnJumpPerformed(InputAction.CallbackContext _)  => OnJump?.Invoke();
    private void OnCrouchStarted(InputAction.CallbackContext _)  => OnCrouch?.Invoke(true);
    private void OnCrouchCanceled(InputAction.CallbackContext _) => OnCrouch?.Invoke(false);
    private void OnRunStarted(InputAction.CallbackContext _)     => OnRun?.Invoke(true);
    private void OnRunCanceled(InputAction.CallbackContext _)    => OnRun?.Invoke(false);
    private void OnGrabStarted(InputAction.CallbackContext _)    => OnGrab?.Invoke(true);
    private void OnGrabCanceled(InputAction.CallbackContext _)   => OnGrab?.Invoke(false);
}
