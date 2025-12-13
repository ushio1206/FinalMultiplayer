using Unity.Netcode;
using UnityEngine;

public class PlayerTeam : NetworkBehaviour
{
    public static PlayerTeam LocalInstance;

    public NetworkVariable<int> team = new NetworkVariable<int>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            team.Value = IsServer ? 0 : 1; // 0 = Izquierda | 1 = Derecha
        }
    }
}
