using UnityEngine;

public class PushableBlock : MonoBehaviour
{
    [Header("Configuración")]
    public float moveSpeed = 2f;
    public float snapDistance = 0.05f; // Distancia mínima para hacer snap a grid

    private Rigidbody rb;
    private bool isBeingPushed = false;
    private Vector3 allowedAxis; // El eje en que se puede mover este bloque en este agarre
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
        rb.constraints = RigidbodyConstraints.FreezeRotation;
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

    private void FixedUpdate()
    {
        if (!isBeingPushed) return;

        rb.linearVelocity = allowedAxis * inputValue * moveSpeed;
    }

    // Hace snap al grid más cercano para que no quede en posiciones extrañas
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