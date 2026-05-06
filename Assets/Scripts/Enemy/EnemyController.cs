using UnityEngine;
using UnityEngine.AI;
using FPS.Weapons;
using FPS.Player;

namespace FPS.Enemy
{
    /// <summary>
    /// Controls enemy behavior — detection, chasing, attacking, taking damage, and death.
    /// Detection uses FOV angle and line-of-sight raycasting.
    /// Supports both melee and ranged attack modes based on EnemyStats.isRanged.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        public EnemyStats stats;

        [Header("Ranged Attack")]
        [Tooltip("Spawn point for bullets. Place at the enemy's gun muzzle.")]
        public Transform bulletSpawnPoint;

        [Tooltip("Bullet prefab to instantiate when shooting.")]
        public GameObject bulletPrefab;

        // State machine
        private enum EnemyState { Idle, Chasing, Attacking, Dead }
        private EnemyState _state = EnemyState.Idle;

        // References
        private NavMeshAgent _agent;
        private Transform _playerTransform;
        private PlayerHealth _playerHealth;
        private float _currentHealth;
        private float _lastAttackTime;

        // Runtime bullet speed override — set by AIDirector
        private float _bulletSpeedOverride = -1f;

        // Events
        public event System.Action<EnemyController> OnEnemyDied;
        public event System.Action OnEnemyAttacked;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _currentHealth = stats.maxHealth;
            ApplyStats();

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerHealth    = player.GetComponent<PlayerHealth>();
            }
            else
            {
                Debug.LogWarning("[EnemyController] No GameObject tagged 'Player' found in scene.");
            }
        }

        private void Update()
        {
            if (_state == EnemyState.Dead) return;
            if (_playerTransform == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);

            switch (_state)
            {
                case EnemyState.Idle:      HandleIdle(distanceToPlayer);      break;
                case EnemyState.Chasing:   HandleChasing(distanceToPlayer);   break;
                case EnemyState.Attacking: HandleAttacking(distanceToPlayer);  break;
            }
        }

        // ── Detection ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the world-space position used as this enemy's eye origin.
        /// </summary>
        private Vector3 GetEyePosition()
        {
            return transform.position + Vector3.up * stats.eyeHeight;
        }

        /// <summary>
        /// Returns true if the player is within FOV angle AND has unobstructed LOS.
        /// </summary>
        private bool CanSeePlayer()
        {
            Vector3 eyePos       = GetEyePosition();
            Vector3 playerCenter = _playerTransform.position + Vector3.up * 0.9f;
            Vector3 dirToPlayer  = (playerCenter - eyePos).normalized;

            // FOV check — angle between forward and direction to player
            float angle = Vector3.Angle(transform.forward, dirToPlayer);
            if (angle > stats.fovAngle) return false;

            // LOS check — raycast must reach the player without hitting anything
            float distance = Vector3.Distance(eyePos, playerCenter);
            if (Physics.Raycast(eyePos, dirToPlayer, out RaycastHit hit, distance))
            {
                // Hit something — only counts as visible if it's the player
                return hit.collider.CompareTag("Player") ||
                       hit.collider.GetComponentInParent<PlayerHealth>() != null;
            }

            // Nothing in the way — player is visible
            return true;
        }

        // ── State Handlers ────────────────────────────────────────────────────

        private void HandleIdle(float distanceToPlayer)
        {
            if (distanceToPlayer <= stats.aggroRange && CanSeePlayer())
            {
                _state = EnemyState.Chasing;
                Debug.Log($"[EnemyController] {name} spotted the player — chasing.");
            }
        }

        private void HandleChasing(float distanceToPlayer)
        {
            if (!_agent.isOnNavMesh) return;

            // Lost sight — return to idle
            if (distanceToPlayer > stats.aggroRange || !CanSeePlayer())
            {
                _state = EnemyState.Idle;
                _agent.ResetPath();
                Debug.Log($"[EnemyController] {name} lost sight of the player — returning to idle.");
                return;
            }

            if (distanceToPlayer <= stats.attackRange)
            {
                _state = EnemyState.Attacking;
                _agent.ResetPath();
                return;
            }

            _agent.SetDestination(_playerTransform.position);
        }

        private void HandleAttacking(float distanceToPlayer)
        {
            // Always face the player
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

            // Player moved out of attack range or behind cover — resume chasing
            if (distanceToPlayer > stats.attackRange || !CanSeePlayer())
            {
                _state = EnemyState.Chasing;
                return;
            }

            if (Time.time >= _lastAttackTime + stats.attackRate)
                Attack();
        }

        // ── Attack ────────────────────────────────────────────────────────────

        private void Attack()
        {
            _lastAttackTime = Time.time;
            OnEnemyAttacked?.Invoke();

            if (stats.isRanged)
                FireBullet();
            else
                MeleeAttack();
        }

        private void MeleeAttack()
        {
            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(stats.attackDamage);
                Debug.Log($"[EnemyController] {name} melee attacked player for {stats.attackDamage}.");
            }
        }

        private void FireBullet()
        {
            if (bulletPrefab == null)
            {
                Debug.LogWarning($"[EnemyController] {name} has no bullet prefab assigned.");
                return;
            }

            Transform spawnFrom  = bulletSpawnPoint != null ? bulletSpawnPoint : transform;
            Vector3 playerCenter = _playerTransform.position + Vector3.up * 0.9f;
            Vector3 aimDirection = (playerCenter - spawnFrom.position).normalized;

            // Keep bullets horizontal — no downward arc
            aimDirection = new Vector3(aimDirection.x, 0f, aimDirection.z).normalized;

            GameObject bulletGO  = Instantiate(bulletPrefab, spawnFrom.position, Quaternion.LookRotation(aimDirection));
            EnemyBullet bullet   = bulletGO.GetComponent<EnemyBullet>();

            if (bullet != null)
            {
                float speed = _bulletSpeedOverride > 0f ? _bulletSpeedOverride : stats.bulletSpeed;
                bullet.Init(stats.attackDamage, speed);
                Debug.Log($"[EnemyController] {name} fired bullet at speed {speed}.");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Called by the AI Director to change bullet speed at runtime.</summary>
        public void SetBulletSpeed(float speed) => _bulletSpeedOverride = speed;

        public bool TakeDamage(float damage)
        {
            if (_state == EnemyState.Dead) return false;

            _currentHealth = Mathf.Max(_currentHealth - damage, 0f);
            Debug.Log($"[EnemyController] {name} took {damage} | HP: {_currentHealth}/{stats.maxHealth}");

            // Taking damage always breaks idle
            if (_state == EnemyState.Idle)
                _state = EnemyState.Chasing;

            if (_currentHealth <= 0f)
            {
                Die();
                return true;
            }

            return false;
        }

        private void Die()
        {
            _state = EnemyState.Dead;
            _agent.isStopped = true;
            OnEnemyDied?.Invoke(this);
            Debug.Log($"[EnemyController] {name} died.");
            Destroy(gameObject, 0.5f);
        }

        public void ApplyStats()
        {
            _agent.speed            = stats.moveSpeed;
            _agent.stoppingDistance = stats.stoppingDistance;
            _currentHealth          = stats.maxHealth;
        }

        public void ForceAggro()
        {
            if (_state == EnemyState.Dead) return;
            _state = EnemyState.Chasing;
            Debug.Log($"[EnemyController] {name} force aggroed.");
        }

        public float GetCurrentHealth() => _currentHealth;

        private void OnDrawGizmosSelected()
        {
            Vector3 eyePos = transform.position + Vector3.up * (stats != null ? stats.eyeHeight : 1.4f);

            // Aggro range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(eyePos, stats != null ? stats.aggroRange : 15f);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(eyePos, stats != null ? stats.attackRange : 2f);

            // FOV cone — draw two lines representing the FOV edges
            if (stats != null)
            {
                Gizmos.color = Color.cyan;
                float halfFov = stats.fovAngle;
                Vector3 leftBound  = Quaternion.Euler(0, -halfFov, 0) * transform.forward * stats.aggroRange;
                Vector3 rightBound = Quaternion.Euler(0,  halfFov, 0) * transform.forward * stats.aggroRange;
                Gizmos.DrawRay(eyePos, leftBound);
                Gizmos.DrawRay(eyePos, rightBound);
            }
        }
    }
}
