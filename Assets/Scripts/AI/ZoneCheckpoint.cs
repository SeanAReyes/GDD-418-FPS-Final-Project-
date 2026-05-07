using UnityEngine;

namespace FPS.AI
{
    /// <summary>
    /// Fires OnPlayerCrossed while the player is inside, but only when
    /// approaching from the Zone N side (transform.forward points toward Zone N+1).
    /// </summary>
    /// <summary>
/// Fires OnPlayerCrossed while the player is inside, but only when
/// approaching from the Zone N side. advanceDirection (world-space) must
/// point toward the NEXT zone — independent of the GameObject's rotation.
/// </summary>
    public class ZoneCheckpoint : MonoBehaviour
{
        [Tooltip("Which zone index this checkpoint belongs to (0-based).")]
        public int zoneIndex;
        public event System.Action<int> OnPlayerCrossed;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            Debug.Log($"[ZoneCheckpoint] Player entered zone {zoneIndex} checkpoint ");
            OnPlayerCrossed?.Invoke(zoneIndex);
                
        }
    }

}