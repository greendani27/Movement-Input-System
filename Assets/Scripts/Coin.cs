using UnityEngine;

/// <summary>
/// Colócalo en el GameObject moneda.
/// Requiere un Collider con "Is Trigger" activado.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Coin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (other.TryGetComponent(out CoinInventory inventory))
            inventory.AddCoin();

        gameObject.SetActive(false);
    }
}
