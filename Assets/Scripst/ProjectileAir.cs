using Unity.Netcode;
using UnityEngine;

public class ProjectileAir : NetworkBehaviour
{
    public float speed = 10f;
    public int damage = 10;
    public float maxLifetime = 2f;

    private Vector3 direction;
    private int ownerTeam; // ← usar int para coincidir con Soldier y Tower

    private float timer;

    public void Initialize(Vector3 dir, int team)
    {
        direction = dir.normalized;
        ownerTeam = team;
    }

    private void Update()
    {
        if (!IsServer) return;

        transform.position += direction * speed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= maxLifetime)
            SafeDespawn();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!IsServer) return;

        // 🟦 Golpear SOLDADO enemigo
        if (col.TryGetComponent<Soldier>(out Soldier soldier))
        {
            if (soldier.team.Value != ownerTeam) // evitar equipo propio
            {
                soldier.TakeDamageServerRpc(damage);
                SafeDespawn();
            }
            return;
        }

        // 🟧 Golpear TORRE enemiga
        if (col.TryGetComponent<Tower>(out Tower tower))
        {
            if (tower.team.Value != ownerTeam)
            {
                tower.TakeDamageServerRpc(damage);
                SafeDespawn();
            }
            return;
        }

        // Opcional: si golpea cualquier otra cosa
        // SafeDespawn();
    }

    private void SafeDespawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
