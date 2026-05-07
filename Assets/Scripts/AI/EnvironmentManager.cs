using UnityEngine;

namespace FPS.AI
{
    /// <summary>
    /// Manages dynamic environment changes driven by the AI Director.
    /// One set of obstacles per zone: active by default, disabled when player dominates,
    /// re-enabled when player struggles.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Default Obstacles")]
        [Tooltip("Fallback obstacles used before the first zone loads.")]
        public GameObject[] removableObstacles;

        private int _removableIndex;

        private void Start()
        {
            _removableIndex = 0;
        }

        /// <summary>
        /// Called by ZoneManager at the start of each zone to swap obstacle sets.
        /// </summary>
        public void SetZoneObstacles(GameObject[] removable)
        {
            ResetEnvironment();
            removableObstacles = removable;
            _removableIndex = 0;
            Debug.Log($"[EnvironmentManager] Zone obstacles updated — {removableObstacles?.Length ?? 0} obstacles.");
        }

        /// <summary>
        /// Called by AIDirector when player is DOMINATING.
        /// Removes cover one at a time — opens the arena.
        /// </summary>
        public void RemoveObstaclesToLimit(int maxToKeep)
        {
            if (removableObstacles == null || removableObstacles.Length == 0) return;
            
            foreach (GameObject obj in removableObstacles)
                if (obj != null) obj.SetActive(true);

            int toDisable = Mathf.Max(0, removableObstacles.Length - maxToKeep);
            for (int i = 0; i < toDisable; i++)
            { 
                if (removableObstacles[i] != null)
                {
                    removableObstacles[i].SetActive(false);
                    Debug.Log($"[EnvironmentManager] Obstacle removed: {removableObstacles[i].name}");
                }
            }

            _removableIndex = toDisable;
            Debug.Log($"[EnvironmentManager] Obstacles limited to {maxToKeep} remaining.");
        }

        /// <summary>
        /// Called by AIDirector when player is STRUGGLING.
        /// Re-enables all obstacles — restores cover.
        /// </summary>
        public void IncreaseObstacles()
        {
            if (removableObstacles == null || removableObstacles.Length == 0) return;

            foreach (GameObject obj in removableObstacles)
                if (obj != null) obj.SetActive(true);

            _removableIndex = 0;
            Debug.Log("[EnvironmentManager] Obstacles restored.");
        }

        /// <summary>Resets all obstacles to active state.</summary>
        public void ResetEnvironment()
        {
            if (removableObstacles != null)
                foreach (GameObject o in removableObstacles)
                    if (o != null) o.SetActive(true);

            _removableIndex = 0;
            Debug.Log("[EnvironmentManager] Environment reset.");
        }
    }
}
