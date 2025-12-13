using Unity.Netcode;
using UnityEngine;

public class Torresarrows : NetworkBehaviour
{
    [Header("Stats")]
    public float speed = 6f;
    public int damage = 10;
    public float lifeTime = 3f;

    private int ownerTeam;
    private Vector3 direction;

    // ===============================
    // INICIALIZAR
    // ===============================
    public void Initialize(Vector3 dir, int team)
    {
        direction = dir.normalized;
        ownerTeam = team;
    }

    private void Start()
    {
        if (IsServer)
            Invoke(nameof(DestroyArrow), lifeTime);
    }

    private void Update()
    {
        if (!IsServer) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    // ===============================
    // COLISIONES (TODOS LOS TIPOS)
    // ===============================
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // SOLDADO NORMAL
        Soldier s = other.GetComponentInParent<Soldier>();
        if (s != null && s.team.Value != ownerTeam)
        {
            s.TakeDamageServerRpc(damage);
            DestroyArrow();
            return;
        }

        // GIGANTE
        SoldierTowerOnly giant = other.GetComponentInParent<SoldierTowerOnly>();
        if (giant != null && giant.team.Value != ownerTeam)
        {
            giant.TakeDamageServerRpc(damage);
            DestroyArrow();
            return;
        }

        // SOLDADO AÉREO
        SoldierAir air = other.GetComponentInParent<SoldierAir>();
        if (air != null && air.team.Value != ownerTeam)
        {
            air.TakeDamageServerRpc(damage);
            DestroyArrow();
            return;
        }

        // ARQUERO
        Archer archer = other.GetComponentInParent<Archer>();
        if (archer != null && archer.team.Value != ownerTeam)
        {
            archer.TakeDamageServerRpc(damage);
            DestroyArrow();
            return;
        }
    }

    // ===============================
    // DESTRUIR
    // ===============================
    private void DestroyArrow()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
