using UnityEngine;

namespace FPS.AI
{
    /// <summary>
    /// Manages dynamic environment changes driven by the AI Director.
    /// Toggles obstacle walls on/off to open or close the arena.
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        [Header("Dynamic Obstacles")]
        [Tooltip("Obstacles the director can disable to open the arena for struggling players.")]
        public GameObject[] removableObstacles;

        [Tooltip("Extra cover objects the director can enable to reward skilled players with more challenge.")]
        public GameObject[] addableObstacles;

        // Tracks current obstacle state
        private int _removableIndex;
        private int _addableIndex;

        private void Start()
        {
            // Ensure addable obstacles start disabled
            foreach (GameObject obstacle in addableObstacles)
            {
                if (obstacle != null)
                    obstacle.SetActive(false);
            }
        }

        /// <summary>
        /// Called by the AI Director when the player is dominating.
        /// Activates the next addable obstacle to increase arena complexity.
        /// </summary>
        public void IncreaseObstacles()
        {
            if (addableObstacles == null || addableObstacles.Length == 0) return;

            if (_addableIndex >= addableObstacles.Length)
            {
                Debug.Log("[EnvironmentManager] All addable obstacles already active.");
                return;
            }

            GameObject obstacle = addableObstacles[_addableIndex];
            if (obstacle != null)
            {
                obstacle.SetActive(true);
                Debug.Log($"[EnvironmentManager] Activated obstacle: {obstacle.name}");
            }

            _addableIndex++;
        }

        /// <summary>
        /// Called by the AI Director when the player is struggling.
        /// Removes the next removable obstacle to open up escape routes.
        /// </summary>
        public void RemoveObstacles()
        {
            if (removableObstacles == null || removableObstacles.Length == 0) return;

            if (_removableIndex >= removableObstacles.Length)
            {
                Debug.Log("[EnvironmentManager] All removable obstacles already removed.");
                return;
            }

            GameObject obstacle = removableObstacles[_removableIndex];
            if (obstacle != null)
            {
                obstacle.SetActive(false);
                Debug.Log($"[EnvironmentManager] Removed obstacle: {obstacle.name}");
            }

            _removableIndex++;
        }

        /// <summary>Resets all obstacles to their original state.</summary>
        public void ResetEnvironment()
        {
            foreach (GameObject obstacle in removableObstacles)
                if (obstacle != null) obstacle.SetActive(true);

            foreach (GameObject obstacle in addableObstacles)
                if (obstacle != null) obstacle.SetActive(false);

            _removableIndex = 0;
            _addableIndex   = 0;

            Debug.Log("[EnvironmentManager] Environment reset.");
        }
    }
}
