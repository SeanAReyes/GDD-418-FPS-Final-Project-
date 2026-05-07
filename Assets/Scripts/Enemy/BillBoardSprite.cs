using UnityEngine;

namespace FPS.Enemy
{
    /// <summary>
    /// Rotates this GameObject's Y axis every frame to face the main camera,
    /// producing a classic billboard sprite effect.
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        private Transform _cam;

        private void Start()
        {
            _cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null) return;

            Vector3 direction = _cam.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
