using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemController : MonoBehaviour
{

    //REVISAR Y ENTENDER BIEN COMO FUNCIONA LO DE LA ROTACION DE LA CAMARA
    private Rigidbody rb;
    private bool isGrounded;
    private float jumpCounter;
    private float coyoteTimeCounter;
    public float coyoteTime;
    public float speed;
    public float jumpheight;
    public float rotationSpeed = 10f;

    private Vector2 moveInput;
    private Transform cameraTransform;

    public InputActionReference move;
    public InputActionReference jump;
    public InputActionReference look;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jumpCounter = 0;
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        moveInput = move.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Dirección de movimiento relativa a la cámara
        Vector3 moveDirection = camForward * moveInput.y + camRight * moveInput.x;

        // Mover manteniendo la velocidad vertical del salto
        rb.linearVelocity = new Vector3(
            moveDirection.x * speed,
            rb.linearVelocity.y,
            moveDirection.z * speed
        );

        // Rotar el personaje hacia donde se mueve
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
    }

    private void OnEnable()
    {
        jump.action.performed += onJumpPerformed;
    }

    private void OnDisable()
    {
        jump.action.performed -= onJumpPerformed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
        coyoteTimeCounter = coyoteTime;
        jumpCounter = 0;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
        coyoteTimeCounter -= Time.deltaTime;
    }

    public void onJumpPerformed(InputAction.CallbackContext context)
    {
        if ((isGrounded || jumpCounter < 2) && coyoteTimeCounter > 0)
        {
            rb.AddForce(Vector3.up * jumpheight, ForceMode.Impulse);
            jumpCounter += 1;
        }
    }
}