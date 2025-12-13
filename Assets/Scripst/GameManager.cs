using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    // 🔒 Evita que el juego termine dos veces
    private bool matchEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ⚠️ Llamado SOLO cuando muere la torre central
    public void OnCentralTowerDestroyed(int loserTeam)
    {
        if (!IsServer) return;
        if (matchEnded) return;

        matchEnded = true;

        Debug.Log("💀 Pierde el equipo: " + loserTeam);

        EndMatchClientRpc(loserTeam);
    }

    // 📡 Avisar a todos los clientes
    [ClientRpc]
    void EndMatchClientRpc(int loserTeam)
    {
        int myTeam = PlayerTeam.LocalInstance.team.Value;

        // 🟢 Mostrar pantalla correcta
        if (UIWinScreen.Instance != null)
        {
            if (myTeam == loserTeam)
                UIWinScreen.Instance.ShowLose();
            else
                UIWinScreen.Instance.ShowWin();
        }

        // 🛑 Bloquear spawns
        SoldierSpawner[] spawners = FindObjectsByType<SoldierSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            spawner.enabled = false;
        }

        // ⏸️ Opcional: pausar el juego
        // Time.timeScale = 0f;
    }
}
