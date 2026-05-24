using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PushableObject : MonoBehaviour, IInteractable
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    public void SetVelocity(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    public void Stop()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.constraints = RigidbodyConstraints.FreezeAll;
    }

    // ── IInteractable ─────────────────────────────────────────────────────────
    public void OnLookAt() { }
    public void OnLookAway() { }
}