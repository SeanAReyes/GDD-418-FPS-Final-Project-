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
        [Tooltip("Radius within which the enemy detects and chases the player.")]
        public float aggroRange = 15f;

        [Header("Attack")]
        [Tooltip("Damage dealt to the player per attack.")]
        public float attackDamage = 10f;

        [Tooltip("Seconds between each attack.")]
        public float attackRate = 1.5f;

        [Tooltip("Range within which the enemy can land an attack.")]
        public float attackRange = 2f;
    }
}
