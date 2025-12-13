using Unity.Netcode;
using UnityEngine;

public class Archer : NetworkBehaviour
{
    [Header("Stats")]
    public int maxHealth = 60;
    public float attackDamage = 8f;
    public float attackRange = 4f;
    public float attackSpeed = 1.2f;
    public float moveSpeed = 2f;

    [Header("Team")]
    public NetworkVariable<int> team = new NetworkVariable<int>();

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private GameObject target;
    private float attackCooldown = 0f;

    private Animator anim;

    [Header("Projectile Settings")]
    public GameObject arrowPrefab;
    public Transform firePoint;

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

        if (target == null)
            target = FindEnemyTarget();

        if (target == null)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        RotateTowardsTarget();

        if (dist > attackRange)
        {
            MoveTowardsTarget();
            if (anim != null) anim.SetBool("isMoving", true);
        }
        else
        {
            if (anim != null) anim.SetBool("isMoving", false);

            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f)
            {
                Attack(target);
                attackCooldown = attackSpeed;
            }
        }
    }

    // 🏃 Movimiento hacia objetivo
    void MoveTowardsTarget()
    {
        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    // 🔄 Rotación izquierda/derecha (2D real)
    void RotateTowardsTarget()
    {
        if (target == null) return;

        Vector3 dir = target.transform.position - transform.position;

        transform.localScale = (dir.x >= 0) ?
            new Vector3(1, 1, 1) :
            new Vector3(-1, 1, 1);
    }

    // 🎯 Buscar enemgios terrestres, aéreos y torres
    GameObject FindEnemyTarget()
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        // 1️⃣ Buscar SOLDADOS terrestres
        Soldier[] soldiers = FindObjectsOfType<Soldier>();
        foreach (var s in soldiers)
        {
            if (s.team.Value == team.Value) continue;

            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = s.gameObject;
            }
        }

        // 2️⃣ Buscar SOLDADOS aéreos
        if (closest == null)
        {
            SoldierAir[] airUnits = FindObjectsOfType<SoldierAir>();
            foreach (var a in airUnits)
            {
                if (a.team.Value == team.Value) continue;

                float d = Vector3.Distance(transform.position, a.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = a.gameObject;
                }
            }
        }

        // 3️⃣ Buscar TORRES
        if (closest == null)
        {
            Tower[] towers = FindObjectsOfType<Tower>();
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
        }

        return closest;
    }

    // 🏹 ATACAR → dispara una flecha
    void Attack(GameObject obj)
    {
        if (anim != null)
            anim.SetTrigger("attack");

        ShootArrow();
    }

    // 🏹 Crear flecha
    void ShootArrow()
    {
        if (!IsServer) return;
        if (arrowPrefab == null || firePoint == null) return;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

        arrow.GetComponent<ProjectileAir>().Initialize(
            firePoint.right,
            team.Value
        );

        arrow.GetComponent<NetworkObject>().Spawn();
    }

    // 💥 Recibir daño
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
            NetworkObject.Despawn();
    }
}

