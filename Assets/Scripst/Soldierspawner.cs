using Unity.Netcode;
using UnityEngine;

public class SoldierSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    public GameObject normalSoldierPrefab;
    public GameObject giantSoldierPrefab;
    public GameObject airSoldierPrefab;
    public GameObject archerPrefab;

    // ======== MÉTODOS PARA UI ========
    public void SpawnNormalFromUI() => SpawnNormalSoldier();
    public void SpawnGiantFromUI() => SpawnGiantSoldier();
    public void SpawnAirFromUI() => SpawnAirSoldier();
    public void SpawnArcherFromUI() => SpawnArcher();

    [Header("Spawn")]
    public Transform spawnPoint;

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.X)) SpawnNormalSoldier();
        if (Input.GetKeyDown(KeyCode.Z)) SpawnGiantSoldier();
        if (Input.GetKeyDown(KeyCode.C)) SpawnAirSoldier();
        if (Input.GetKeyDown(KeyCode.V)) SpawnArcher();
    }

    // ============================================================
    void SpawnNormalSoldier()
    {
        if (!IsOwner) return;
        if (PlayerTeam.LocalInstance == null) return;

        int team = PlayerTeam.LocalInstance.team.Value;

        if (IsServer)
            SpawnNormalOnServer(spawnPoint.position, team);
        else
            SpawnNormalServerRpc(spawnPoint.position, team);
    }

    [ServerRpc]
    void SpawnNormalServerRpc(Vector3 pos, int team)
    {
        SpawnNormalOnServer(pos, team);
    }

    void SpawnNormalOnServer(Vector3 pos, int team)
    {
        GameObject s = Instantiate(normalSoldierPrefab, pos, Quaternion.identity);
        s.GetComponent<Soldier>().team.Value = team;
        s.GetComponent<NetworkObject>().Spawn();
    }

    // ============================================================
    void SpawnGiantSoldier()
    {
        if (!IsOwner) return;
        if (PlayerTeam.LocalInstance == null) return;

        int team = PlayerTeam.LocalInstance.team.Value;

        if (IsServer)
            SpawnGiantOnServer(spawnPoint.position, team);
        else
            SpawnGiantServerRpc(spawnPoint.position, team);
    }

    [ServerRpc]
    void SpawnGiantServerRpc(Vector3 pos, int team)
    {
        SpawnGiantOnServer(pos, team);
    }

    void SpawnGiantOnServer(Vector3 pos, int team)
    {
        GameObject g = Instantiate(giantSoldierPrefab, pos, Quaternion.identity);
        g.GetComponent<SoldierTowerOnly>().team.Value = team;
        g.GetComponent<NetworkObject>().Spawn();
    }

    // ============================================================
    void SpawnAirSoldier()
    {
        if (!IsOwner) return;
        if (PlayerTeam.LocalInstance == null) return;

        int team = PlayerTeam.LocalInstance.team.Value;

        if (IsServer)
            SpawnAirOnServer(spawnPoint.position, team);
        else
            SpawnAirServerRpc(spawnPoint.position, team);
    }

    [ServerRpc]
    void SpawnAirServerRpc(Vector3 pos, int team)
    {
        SpawnAirOnServer(pos, team);
    }

    void SpawnAirOnServer(Vector3 pos, int team)
    {
        GameObject a = Instantiate(airSoldierPrefab, pos, Quaternion.identity);
        a.GetComponent<SoldierAir>().team.Value = team;
        a.GetComponent<NetworkObject>().Spawn();
    }

    // ============================================================
    void SpawnArcher()
    {
        if (!IsOwner) return;
        if (PlayerTeam.LocalInstance == null) return;

        int team = PlayerTeam.LocalInstance.team.Value;

        if (IsServer)
            SpawnArcherOnServer(spawnPoint.position, team);
        else
            SpawnArcherServerRpc(spawnPoint.position, team);
    }

    [ServerRpc]
    void SpawnArcherServerRpc(Vector3 pos, int team)
    {
        SpawnArcherOnServer(pos, team);
    }

    void SpawnArcherOnServer(Vector3 pos, int team)
    {
        GameObject ar = Instantiate(archerPrefab, pos, Quaternion.identity);
        ar.GetComponent<Archer>().team.Value = team;
        ar.GetComponent<NetworkObject>().Spawn();
    }
}
