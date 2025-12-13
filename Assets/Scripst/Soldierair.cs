using Unity.Netcode;
using UnityEngine;

public class SoldierAir : NetworkBehaviour
{
    [Header("Stats Aéreos")]
    public int maxHealth = 80;
    public float attackDamage = 12f;
    public float attackRange = 3f;
    public float attackSpeed = 1.2f;
    public float flySpeed = 3.5f;

    [Header("Team")]
    public NetworkVariable<int> team = new NetworkVariable<int>();

    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private float attackCooldown = 0f;

    private GameObject target;
    private Animator anim;

    [Header("Projectile Settings")]
    public GameObject projectilePrefab;   // ← Bala
    public Transform firePoint;           // ← Lugar donde sale la bala

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

        // Buscar target si no existe
        if (target == null)
            target = FindEnemyTarget();

        if (target == null)
        {
            if (anim != null) anim.SetBool("isMoving", false);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);

        RotateTowardsTarget();

        // ✈️ MOVERSE
        if (dist > attackRange)
        {
            FlyToTarget();
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

    // ✈️ Movimiento hacia objetivo
    void FlyToTarget()
    {
        if (target == null) return;

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * flySpeed * Time.deltaTime;
    }

    // 🔄 Rotación SOLO izquierda/derecha
    void RotateTowardsTarget()
    {
        if (target == null) return;

        Vector3 dir = target.transform.position - transform.position;

        if (dir.x >= 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    // 🔍 Buscar primero soldados enemigos, luego torres
    GameObject FindEnemyTarget()
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;

        // 1️⃣ Buscar soldados enemigos
        Soldier[] allSoldiers = FindObjectsOfType<Soldier>();
        foreach (var s in allSoldiers)
        {
            if (s.team.Value == team.Value) continue;
            if (s.gameObject == this.gameObject) continue;

            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = s.gameObject;
            }
        }

        if (closest != null)
            return closest;

        // 2️⃣ Buscar torres si no hay soldados
        Tower[] towers = FindObjectsOfType<Tower>();
        minDist = Mathf.Infinity;

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

    // 📌 ATAQUE → disparar bala
    void Attack(GameObject obj)
    {
        if (anim != null)
            anim.SetTrigger("attack");

        ShootProjectile();
    }

    // 🔫 Instanciar proyectil (bala)
    void ShootProjectile()
    {
        if (!IsServer) return;
        if (projectilePrefab == null || firePoint == null) return;

        GameObject proj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        proj.GetComponent<ProjectileAir>().Initialize(
            firePoint.right,   // dirección del disparo
            team.Value         // equipo dueño de la bala
        );

        proj.GetComponent<NetworkObject>().Spawn();
    }

    // 📉 Recibir daño
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
            NetworkObject.Despawn();
    }
}
