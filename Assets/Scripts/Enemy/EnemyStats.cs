using UnityEngine;

namespace FPS.Enemy
{
    [CreateAssetMenu(fileName = "EnemyStats", menuName = "FPS/Enemy/Enemy Stats")]
    public class EnemyStats : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Total health this enemy starts with.")]
        public float maxHealth = 50f;

        [Header("Movement")]
        [Tooltip("NavMeshAgent movement speed.")]
        public float moveSpeed = 3.5f;

        [Tooltip("How close the enemy gets before stopping to attack.")]
        public float stoppingDistance = 2f;

        [Header("Detection")]
        [Tooltip("Radius within which the enemy can detect the player.")]
        public float aggroRange = 15f;

        [Tooltip("Field of view half-angle in degrees. Player must be within this cone to be detected.")]
        [Range(10f, 180f)]
        public float fovAngle = 90f;

        [Tooltip("Vertical offset from the enemy's root position used as the eye origin for LOS checks.")]
        public float eyeHeight = 1.4f;

        [Header("Attack")]
        [Tooltip("Damage dealt per attack or bullet.")]
        public float attackDamage = 10f;

        [Tooltip("Seconds between each attack or shot.")]
        public float attackRate = 1.5f;

        [Tooltip("Range within which the enemy attacks or starts shooting.")]
        public float attackRange = 2f;

        [Header("Ranged")]
        [Tooltip("If true, this enemy fires bullets instead of melee attacking.")]
        public bool isRanged = false;

        [Tooltip("Speed of the bullet projectile. AI Director adjusts this at runtime.")]
        public float bulletSpeed = 8f;
    }
}
