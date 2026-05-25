using UnityEngine;

/// <summary>
/// Wrapper del Animator del jugador.
/// El resto de scripts llaman a estos métodos en lugar de tocar el Animator directamente.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimatorController : MonoBehaviour
{
    private Animator animator;

    // Hashes para evitar string lookups en runtime
    private static readonly int HashSpeed  = Animator.StringToHash("Speed");
    private static readonly int HashJump   = Animator.StringToHash("Jump");
    private static readonly int HashCrouch = Animator.StringToHash("Crouch");
    private static readonly int HashPush   = Animator.StringToHash("Push");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void SetSpeed(float speed)   => animator.SetFloat(HashSpeed,  speed);
    public void SetJump(bool value)     => animator.SetBool(HashJump,    value);
    public void SetCrouch(bool value)   => animator.SetBool(HashCrouch,  value);
    public void SetPush(bool value)     => animator.SetBool(HashPush,    value);
}
