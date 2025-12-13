using Unity.Netcode;
using UnityEngine;

public class Tower : NetworkBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Team")]
    public NetworkVariable<int> team = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Tipo de torre")]
    [SerializeField] private bool isCentral = false; // Torre Rey

    [Header("Defense Settings")]
    public float attackRange = 5f;
    public float attackCooldown = 1.5f;
    public GameObject arrowPrefab;
    public Transform firePoint;

    private float attackTimer;
    private bool destroyed = false;

    private void Start()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    private void Update()
    {
        if (!IsServer || destroyed) return;

        attackTimer -= Time.deltaTime;

        if (attackTimer <= 0f)
        {
            GameObject target = FindEnemyTarget();
            if (target != null)
            {
                ShootArrow(target);
                attackTimer = attackCooldown;
            }
        }
    }

    // ===============================
    // BUSCAR ENEMIGO
    // ===============================
    GameObject FindEnemyTarget()
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        void Check(GameObject obj, int objTeam)
        {
            float d = Vector3.Distance(transform.position, obj.transform.position);
            if (objTeam != team.Value && d <= attackRange && d < minDist)
            {
                minDist = d;
                closest = obj;
            }
        }

        foreach (var s in FindObjectsOfType<Soldier>())
            Check(s.gameObject, s.team.Value);

        foreach (var a in FindObjectsOfType<SoldierAir>())
            Check(a.gameObject, a.team.Value);

        foreach (var ar in FindObjectsOfType<Archer>())
            Check(ar.gameObject, ar.team.Value);

        return closest;
    }

    // ===============================
    // DISPARAR FLECHA (USANDO TORRESARROWS)
    // ===============================
    void ShootArrow(GameObject target)
    {
        if (arrowPrefab == null || firePoint == null) return;

        GameObject arrow = Instantiate(
            arrowPrefab,
            firePoint.position,
            Quaternion.identity
        );

        // 🔥 SCRIPT CORRECTO
        Torresarrows arrowScript = arrow.GetComponent<Torresarrows>();
        NetworkObject netObj = arrow.GetComponent<NetworkObject>();

        if (arrowScript == null || netObj == null)
        {
            Debug.LogError("❌ Flecha mal configurada (falta Torresarrows o NetworkObject)");
            Destroy(arrow);
            return;
        }

        Vector3 dir = (target.transform.position - firePoint.position).normalized;
        arrowScript.Initialize(dir, team.Value);

        netObj.Spawn();
    }

    // ===============================
    // DAÑO
    // ===============================
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (destroyed || damage <= 0) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            DestroyTower();
        }
    }

    // ===============================
    // DESTRUIR TORRE
    // ===============================
    void DestroyTower()
    {
        if (destroyed) return;
        destroyed = true;

        if (isCentral && GameManager.Instance != null)
            GameManager.Instance.OnCentralTowerDestroyed(team.Value);

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }

    // ===============================
    // GETTERS
    // ===============================
    public int GetHealth() => currentHealth.Value;
    public bool IsCentralTower() => isCentral;
}
