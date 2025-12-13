using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    public float speed = 6f;
    public int damage = 10;

    // El equipo del soldado que disparó
    public int team;

    // Tiempo máximo para auto-destruirse
    public float lifeTime = 3f;

    private void Start()
    {
        if (IsServer)
            Invoke(nameof(DestroyBullet), lifeTime);
    }

    private void Update()
    {
        if (!IsServer) return;

        // Mover la bala hacia adelante
        transform.position += transform.right * speed * Time.deltaTime;
    }

    // Detectar colisiones
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // 🟨 Golpea TORRE enemiga
        if (other.TryGetComponent<Tower>(out Tower tower))
        {
            if (tower.team.Value != team) // evitar daño propio
            {
                tower.TakeDamageServerRpc(damage);
                DestroyBullet();
            }
        }

        // 🟧 Golpea SOLDADO enemigo
        if (other.TryGetComponent<Soldier>(out Soldier soldier))
        {
            if (soldier.team.Value != team)
            {
                soldier.TakeDamageServerRpc(damage);
                DestroyBullet();
            }
        }
    }

    private void DestroyBullet()
    {
        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }
}
