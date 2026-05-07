using UnityEngine;

namespace FPS.Player
{
    /// <summary>
    /// Manages player health, damage intake, and death.
    /// Implements IDamageable so enemies can hit the player
    /// using the same interface as any other target.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, FPS.Weapons.IDamageable
    {
        [Header("Health")]
        [Tooltip("Maximum health the player starts with.")]
        public float maxHealth = 100f;

        // Current health value
        private float _currentHealth;

        // Events for the AI director and UI to subscribe to
        public event System.Action<float, float> OnHealthChanged; // current, max
        public event System.Action OnPlayerDied;
        public event System.Action<float> OnDamageTaken;          // damage amount

        private bool _isDead;

        private void Start()
        {
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Called by enemies dealing damage. Returns true if the hit was fatal.
        /// </summary>
        public bool TakeDamage(float damage)
        {
            if (_isDead) return false;

            _currentHealth = Mathf.Max(_currentHealth - damage, 0f);
            OnDamageTaken?.Invoke(damage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            Debug.Log($"[PlayerHealth] Took {damage} damage | HP: {_currentHealth}/{maxHealth}");

            if (_currentHealth <= 0f)
            {
                Die();
                return true;
            }

            return false;
        }

        private void Die()
        {
            _isDead = true;
            OnPlayerDied?.Invoke();
            Debug.Log("[PlayerHealth] Player died.");

            CharacterController cc = gameObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            // Disable player input — movement and shooting stop immediately
            PlayerShootInput shootInput = gameObject.GetComponent<PlayerShootInput>();
            if (shootInput != null) shootInput.enabled = false;
            
            StarterAssets.FirstPersonController fpsController = gameObject.GetComponent<StarterAssets.FirstPersonController>();
            if(fpsController != null) fpsController.enabled = false;
            
        }

        /// <summary>Restores player to full health.</summary>
        public void ResetHealth()
        {
            _isDead = false;
            _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>Returns current health for UI or other systems to read.</summary>
        public float GetCurrentHealth() => _currentHealth;

        /// <summary>Returns max health.</summary>
        public float GetMaxHealth() => maxHealth;

        /// <summary>Returns whether the player is dead.</summary>
        public bool IsDead() => _isDead;
    }
}
