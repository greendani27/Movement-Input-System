using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystemController : MonoBehaviour
{

    private Rigidbody rb;
    public float speed;

    private Vector3 moveDirection;

    public InputActionReference move;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        moveDirection = move.action.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector3(moveDirection.x * speed, rb.linearVelocity.y, moveDirection.y * speed);
    }
}
