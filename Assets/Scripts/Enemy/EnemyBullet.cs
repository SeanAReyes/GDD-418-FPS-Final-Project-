using UnityEngine;
using FPS.Player;

namespace FPS.Enemy
{
    /// <summary>
    /// A visible projectile fired by a ranged enemy.
    /// Moves forward in a straight line, damages the player on contact,
    /// and stops on any obstacle or surface it hits.
    /// </summary>
    public class EnemyBullet : MonoBehaviour
    {
        private float _damage;
        private float _speed;
        private float _lifetime = 5f;
        private float _spawnTime;
        private bool _initialized;

        private void Awake()
        {
            _spawnTime = Time.time;
        }

        /// <summary>Called immediately after Instantiate to configure the bullet.</summary>
        public void Init(float damage, float speed, float lifetime = 5f)
        {
            _damage      = damage;
            _speed       = speed;
            _lifetime    = lifetime;
            _spawnTime   = Time.time;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            transform.position += transform.forward * _speed * Time.deltaTime;

            if (Time.time - _spawnTime >= _lifetime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            // Pass through other enemy bullets and enemies
            if (other.CompareTag("Enemy")) return;

            // Deal damage only if the player was hit
            PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(_damage);

            // Destroy on anything — walls, ground, player, obstacles
            Destroy(gameObject);
        }
    }
}
