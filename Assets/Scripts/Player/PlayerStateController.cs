using UnityEngine;

public enum PlayerState { Idle, Walking, Running, Jumping, Grabbing }

/// <summary>
/// Fuente de verdad del estado del jugador.
/// El resto de scripts leen y escriben el estado a través de esta clase.
/// </summary>
public class PlayerStateController : MonoBehaviour
{
    public PlayerState Current { get; private set; } = PlayerState.Idle;

    public void SetState(PlayerState newState)
    {
        if (Current == newState) return;
        Current = newState;
    }

    public bool Is(PlayerState state) => Current == state;
}
