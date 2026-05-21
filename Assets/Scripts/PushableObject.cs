using UnityEngine;

public class PushableObject : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 1.5f;

    private Rigidbody rb;
    private bool isBeingPushed = false;
    private Vector3 allowedAxis;
    private float inputValue;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void StartPush(Vector3 axis)
    {
        isBeingPushed = true;
        allowedAxis = axis;
    }

    public void StopPush()
    {
        isBeingPushed = false;
        rb.linearVelocity = Vector3.zero;
        SnapToGrid();
    }

    public void SetInput(float value)
    {
        inputValue = value;
    }

    public Vector3 GetVelocity() => rb.linearVelocity;

    private void FixedUpdate()
    {
        if (!isBeingPushed)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        rb.linearVelocity = allowedAxis * inputValue * moveSpeed;
    }

    private void SnapToGrid()
    {
        Vector3 pos = transform.position;
        transform.position = new Vector3(
            Mathf.Round(pos.x),
            pos.y,
            Mathf.Round(pos.z)
        );
    }
}
