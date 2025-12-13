using Unity.Netcode;
using UnityEngine;

public class Soldier : NetworkBehaviour
{
    [Header("Stats")]
    public int maxHealth = 100;
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackSpeed = 1f;
    public float moveSpeed = 2f;

    [Header("Team")]
    public NetworkVariable<int> team = new NetworkVariable<int>();

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();

    private float attackCooldown = 0f;
    private GameObject targetTower;

    // 🔹 ANIMATOR
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

        if (targetTower == null)
            targetTower = FindClosestEnemyTower();

        if (targetTower != null)
        {
            float dist = Vector3.Distance(transform.position, targetTower.transform.position);

            if (dist > attackRange)
            {
                MoveTowardsTarget();
                SetMoving(true);
            }
            else
            {
                SetMoving(false);

                attackCooldown -= Time.deltaTime;

                if (attackCooldown <= 0f)
                {
                    Attack(targetTower);
                    attackCooldown = attackSpeed;
                }
            }
        }
        else
        {
            SetMoving(false);
        }
    }

    // 🔁 Movimiento
    void MoveTowardsTarget()
    {
        Vector3 dir = (targetTower.transform.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // 🎯 Buscar torre enemiga
    GameObject FindClosestEnemyTower()
    {
        Tower[] towers = FindObjectsOfType<Tower>();
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var t in towers)
        {
            if (t.team.Value == team.Value) continue;

            float d = Vector3.Distance(transform.position, t.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = t.gameObject;
            }
        }

        return closest;
    }

    // ⚔ ATAQUE CON ANIMACIÓN
    void Attack(GameObject towerObj)
    {
        if (towerObj.TryGetComponent<Tower>(out Tower t))
        {
            anim.SetTrigger("attack"); // ✅ animación ataque
            t.TakeDamageServerRpc((int)attackDamage);
        }
    }

    // 🏃 CONTROL DE CAMINAR
    void SetMoving(bool value)
    {
        if (anim != null)
            anim.SetBool("isMoving", value);
    }

    // 💥 RECIBIR DAÑO
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
            NetworkObject.Despawn();
    }
}
