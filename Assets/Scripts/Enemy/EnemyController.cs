using UnityEngine;
using UnityEngine.AI;
using FPS.Weapons;
using FPS.Player;

namespace FPS.Enemy
{
    /// <summary>
    /// Controls enemy behavior — detection, chasing, attacking, 
    /// taking damage, and death. Implements IDamageable so the
    /// player's gun can damage it directly.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Data")]
        public EnemyStats stats;

        // State machine
        private enum EnemyState { Idle, Chasing, Attacking, Dead }
        private EnemyState _state = EnemyState.Idle;

        // References
        private NavMeshAgent _agent;
        private Transform _playerTransform;
        private PlayerHealth _playerHealth;
        private float _currentHealth;
        private float _lastAttackTime;

        // Events the AI director and performance tracker subscribe to
        public event System.Action<EnemyController> OnEnemyDied;
        public event System.Action OnEnemyAttacked;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            _currentHealth = stats.maxHealth;

            // Apply stats to the NavMeshAgent
            ApplyStats();

            // Find the player by tag
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerHealth = player.GetComponent<PlayerHealth>();
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
                case EnemyState.Idle:
                    HandleIdle(distanceToPlayer);
                    break;
                case EnemyState.Chasing:
                    HandleChasing(distanceToPlayer);
                    break;
                case EnemyState.Attacking:
                    HandleAttacking(distanceToPlayer);
                    break;
            }
        }

        private void HandleIdle(float distanceToPlayer)
        {
            if (distanceToPlayer <= stats.aggroRange)
            {
                _state = EnemyState.Chasing;
                Debug.Log($"[EnemyController] {name} spotted the player — chasing.");
            }
        }

        private void HandleChasing(float distanceToPlayer)
        {
            if (!_agent.isOnNavMesh) return;
            
            if (distanceToPlayer > stats.aggroRange)
            {
                // Player left aggro range — go back to idle
                _state = EnemyState.Idle;
                _agent.ResetPath();
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
            // Face the player while attacking
            Vector3 direction = (_playerTransform.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0f, direction.z));

            if (distanceToPlayer > stats.attackRange)
            {
                // Player moved out of attack range — resume chasing
                _state = EnemyState.Chasing;
                return;
            }

            if (Time.time >= _lastAttackTime + stats.attackRate)
            {
                Attack();
            }
        }

        private void Attack()
        {
            _lastAttackTime = Time.time;
            OnEnemyAttacked?.Invoke();

            if (_playerHealth != null)
            {
                _playerHealth.TakeDamage(stats.attackDamage);
                Debug.Log($"[EnemyController] {name} attacked player for {stats.attackDamage} damage.");
            }
        }

        /// <summary>
        /// Called by the player's GunController via IDamageable.
        /// Returns true if the hit was fatal.
        /// </summary>
        public bool TakeDamage(float damage)
        {
            if (_state == EnemyState.Dead) return false;

            _currentHealth = Mathf.Max(_currentHealth - damage, 0f);
            Debug.Log($"[EnemyController] {name} took {damage} damage | HP: {_currentHealth}/{stats.maxHealth}");

            // Getting hit triggers chase immediately
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

            // Destroy after a short delay so death events can propagate
            Destroy(gameObject, 0.5f);
        }

        /// <summary>
        /// Called by the AI director to apply a different EnemyStats preset at runtime.
        /// </summary>
        public void ApplyStats()
        {
            _agent.speed = stats.moveSpeed;
            _agent.stoppingDistance = stats.stoppingDistance;
            _currentHealth = stats.maxHealth;
        }

        /// <summary>
        /// Forces the enemy to immediately start chasing the player.
        /// Called by the AI director when the player is disengaging.
        /// </summary>
        public void ForceAggro()
        {
            if (_state == EnemyState.Dead) return;
            _state = EnemyState.Chasing;
            Debug.Log($"[EnemyController] {name} force aggroed.");
        }

        /// <summary>Returns current health for external systems.</summary>
        public float GetCurrentHealth() => _currentHealth;

        private void OnDrawGizmosSelected()
        {
            // Visualise aggro and attack ranges in the scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats != null ? stats.aggroRange : 15f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats != null ? stats.attackRange : 2f);
        }
    }
}
