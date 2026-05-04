using System.Collections;
using UnityEngine;

namespace FPS.Weapons
{
    /// <summary>
    /// Handles firing logic, ammo tracking, bullet tracer, and muzzle flash
    /// for a raycast-based hitscan weapon.
    /// </summary>
    public class GunController : MonoBehaviour
    {
        [Header("Data")]
        public GunData gunData;

        [Header("References")]
        [Tooltip("The point bullets are fired from and muzzle flash spawns.")]
        public Transform muzzlePoint;

        [Tooltip("Camera used to aim the raycast. Assign the Main Camera.")]
        public Camera aimCamera;

        [Header("Muzzle Flash")]
        [Tooltip("Optional particle system for muzzle flash.")]
        public ParticleSystem muzzleFlash;

        [Header("Bullet Tracer")]
        [Tooltip("LineRenderer used to draw the bullet tracer.")]
        public LineRenderer bulletTracer;

        // State
        private int _currentAmmo;
        private float _nextFireTime;
        private bool _isReloading;

        // Events other systems (AI director, performance tracker) can listen to
        public event System.Action OnFired;
        public event System.Action OnKill;
        public event System.Action OnReloadStart;
        public event System.Action OnReloadEnd;

        private void Start()
        {
            _currentAmmo = gunData.magazineSize;

            if (bulletTracer != null)
                bulletTracer.enabled = false;
        }

        /// <summary>Called by PlayerShootInput every frame the fire button is held.</summary>
        public void TryFire()
        {
            if (_isReloading) return;
            if (Time.time < _nextFireTime) return;

            if (_currentAmmo <= 0)
            {
                StartCoroutine(Reload());
                return;
            }

            Fire();
        }

        private void Fire()
        {
            _currentAmmo--;
            _nextFireTime = Time.time + 1f / gunData.fireRate;

            PlayMuzzleFlash();

            // Raycast from screen center outward
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 hitPoint;

            if (Physics.Raycast(ray, out RaycastHit hit, gunData.range))
            {
                hitPoint = hit.point;
                HandleHit(hit);
            }
            else
            {
                hitPoint = ray.origin + ray.direction * gunData.range;
            }

            StartCoroutine(ShowTracer(muzzlePoint.position, hitPoint));
            OnFired?.Invoke();

            Debug.Log($"[GunController] Fired | Ammo: {_currentAmmo}/{gunData.magazineSize}");
        }

        private void HandleHit(RaycastHit hit)
        {
            // Damageable interface hook — enemies will implement this later
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                bool killed = damageable.TakeDamage(gunData.damage);
                if (killed)
                {
                    OnKill?.Invoke();
                    Debug.Log($"[GunController] Killed: {hit.collider.name}");
                }
            }
        }

        private void PlayMuzzleFlash()
        {
            if (muzzleFlash != null)
                muzzleFlash.Play();
        }

        private IEnumerator ShowTracer(Vector3 start, Vector3 end)
        {
            if (bulletTracer == null) yield break;

            bulletTracer.enabled = true;
            bulletTracer.SetPosition(0, start);
            bulletTracer.SetPosition(1, end);

            yield return new WaitForSeconds(gunData.tracerDuration);

            bulletTracer.enabled = false;
        }

        private IEnumerator Reload()
        {
            _isReloading = true;
            OnReloadStart?.Invoke();
            Debug.Log($"[GunController] Reloading...");

            yield return new WaitForSeconds(gunData.reloadTime);

            _currentAmmo = gunData.magazineSize;
            _isReloading = false;
            OnReloadEnd?.Invoke();
            Debug.Log($"[GunController] Reload complete.");
        }

        /// <summary>Returns current ammo for UI or other systems to read.</summary>
        public int GetCurrentAmmo() => _currentAmmo;

        /// <summary>Returns the magazine size from GunData.</summary>
        public int GetMagazineSize() => gunData.magazineSize;

        /// <summary>Returns whether the gun is currently reloading.</summary>
        public bool IsReloading() => _isReloading;

        public void TryReload()
        {
            if (_isReloading) return;
            if (_currentAmmo >= gunData.magazineSize) return;

            StartCoroutine(Reload());
        }
    }
}
