using UnityEngine;

/// <summary>
/// Detecta objetos IInteractable mirando al frente y gestiona la UI de interacción.
/// Completamente independiente del movimiento.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float      interactCheckDistance = 2f;
    [SerializeField] private GameObject interactUI;

    private IInteractable currentLookedAt = null;

    // ════════════════════════════════════════════════════════════════════════
    // Update
    // ════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        CheckInteractableLook();
    }

    // ════════════════════════════════════════════════════════════════════════
    // Lógica privada
    // ════════════════════════════════════════════════════════════════════════

    private void CheckInteractableLook()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactCheckDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentLookedAt != interactable)
                {
                    currentLookedAt = interactable;
                    interactUI.SetActive(true);
                }
                return;
            }
        }

        if (currentLookedAt != null)
        {
            currentLookedAt = null;
            interactUI.SetActive(false);
        }
    }
}
