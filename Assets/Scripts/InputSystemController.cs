using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemController : MonoBehaviour
{

    private Rigidbody rb;
    private bool isGrounded;
    private float jumpCounter;

    private float coyoteTimeCounter;
    public float coyoteTime;

    public float speed;
    public float jumpheight;
    

    private Vector3 moveDirection;

    public InputActionReference move;
    public InputActionReference jump;
    public InputActionReference look;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        jumpCounter = 0;
    }

    void Update()
    {
        moveDirection = move.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(moveDirection.x * speed, rb.linearVelocity.y, moveDirection.y * speed);
    }

    private void OnEnable()
    {
        jump.action.performed += onJumpPerformed;
        look.action.performed += onLookPerformed;
    }

    private void OnDisable()
    {
        jump.action.performed -= onJumpPerformed;
        look.action.performed -= onLookPerformed;
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

    public void onLookPerformed(InputAction.CallbackContext context) {
        
    }
}
