using Unity.Netcode;
using UnityEngine;

public class SoldierTowerOnly : NetworkBehaviour
{
    [Header("Stats")]
    public int maxHealth = 120;
    public float attackDamage = 20f;
    public float attackRange = 1.5f;
    public float attackSpeed = 1.2f;
    public float moveSpeed = 1.8f;

    [Header("Team")]
    public NetworkVariable<int> team = new NetworkVariable<int>();

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    private float attackCooldown = 0f;
    private GameObject targetTower;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        if (IsServer)
            currentHealth.Value = maxHealth;
    }

    private void Update()
    {
        if (!IsServer) return;

        // ✅ SOLO BUSCA TORRES (NUNCA SOLDADOS)
        if (targetTower == null)
            targetTower = FindClosestEnemyTower();

        if (targetTower == null) return;

        float dist = Vector3.Distance(transform.position, targetTower.transform.position);

        // 🏃 CAMINAR
        if (dist > attackRange)
        {
            MoveTowardsTarget();

            if (anim != null)
                anim.SetBool("isMoving", true);
        }
        // 🏗️ ATACAR TORRE
        else
        {
            if (anim != null)
                anim.SetBool("isMoving", false);

            attackCooldown -= Time.deltaTime;

            if (attackCooldown <= 0f)
            {
                Attack(targetTower);
                attackCooldown = attackSpeed;
            }
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 dir = (targetTower.transform.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    GameObject FindClosestEnemyTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var t in towers)
        {
            if (t.team.Value == team.Value) continue; // ❌ no atacar torres aliadas

            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = t.gameObject;
            }
        }

        return closest;
    }

    void Attack(GameObject towerObj)
    {
        if (towerObj.TryGetComponent<Tower>(out Tower t))
        {
            if (anim != null)
                anim.SetTrigger("attack");

            t.TakeDamageServerRpc((int)attackDamage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
            NetworkObject.Despawn();
    }
}
