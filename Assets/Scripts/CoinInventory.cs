using UnityEngine;

/// <summary>
/// Inventario de monedas del jugador.
/// Vive en el mismo GameObject que el resto de scripts del jugador.
/// </summary>
public class CoinInventory : MonoBehaviour
{
    public int CoinCount { get; private set; } = 0;

    public void AddCoin()
    {
        CoinCount++;
        Debug.Log($"Monedas: {CoinCount}");
    }
}
