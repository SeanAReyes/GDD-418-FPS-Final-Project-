using UnityEngine;

namespace FPS.Weapons
{
    [CreateAssetMenu(fileName = "GunData", menuName = "FPS/Weapons/Gun Data")]
    public class GunData : ScriptableObject
    {
        [Header("Identity")]
        public string gunName = "Default Gun";

        [Header("Firing")]
        [Tooltip("Damage dealt per hit.")]
        public float damage = 25f;

        [Tooltip("Maximum distance the raycast will detect a hit.")]
        public float range = 100f;

        [Tooltip("Rounds per second the gun can fire.")]
        public float fireRate = 10f;

        [Header("Ammo")]
        [Tooltip("Maximum rounds in one magazine.")]
        public int magazineSize = 30;

        [Tooltip("Time in seconds it takes to reload.")]
        public float reloadTime = 1.5f;

        [Header("Visual")]
        [Tooltip("Time in seconds the bullet tracer line remains visible.")]
        public float tracerDuration = 0.05f;
    }
}
