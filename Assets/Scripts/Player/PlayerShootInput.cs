using UnityEngine;
using StarterAssets;
using FPS.Weapons;

namespace FPS.Player
{
    /// <summary>
    /// Reads the Fire input from StarterAssetsInputs and forwards
    /// it to the GunController each frame.
    /// </summary>
    public class PlayerShootInput : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The GunController on the equipped weapon.")]
        public GunController equippedGun;

        private StarterAssetsInputs _input;

        private void Awake()
        {
            _input = GetComponent<StarterAssetsInputs>();

            if (_input == null)
                Debug.LogError("[PlayerShootInput] StarterAssetsInputs not found on this GameObject.");

            if (equippedGun == null)
                Debug.LogError("[PlayerShootInput] No GunController assigned to PlayerShootInput.");
        }

        private void Update()
        {
            if (equippedGun == null || _input == null) return;

            if (_input.fire)
                equippedGun.TryFire();
            if (_input.reload)
                equippedGun.TryReload();
        }
    }
}
