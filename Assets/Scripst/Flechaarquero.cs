using Unity.Netcode;
using UnityEngine;

public class ArrowProjectile : NetworkBehaviour
{
    public float speed = 12f;
    public int damage = 10;
    public float maxLifetime = 2f;

    private Vector3 direction;
    private int ownerTeam;
    private float timer = 0f;

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

        // 🟦 Golpear soldado terrestre
        if (col.TryGetComponent<Soldier>(out Soldier s))
        {
            if (s.team.Value != ownerTeam)
            {
                s.TakeDamageServerRpc(damage);
                SafeDespawn();
            }
            return;
        }

        // 🟧 Golpear soldado aéreo
        if (col.TryGetComponent<SoldierAir>(out SoldierAir a))
        {
            if (a.team.Value != ownerTeam)
            {
                a.TakeDamageServerRpc(damage);
                SafeDespawn();
            }
            return;
        }

        // 🟥 Golpear torre
        if (col.TryGetComponent<Tower>(out Tower t))
        {
            if (t.team.Value != ownerTeam)
            {
                t.TakeDamageServerRpc(damage);
                SafeDespawn();
            }
            return;
        }
    }

    // Despawn seguro
    private void SafeDespawn()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
