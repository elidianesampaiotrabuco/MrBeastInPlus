using UnityEngine;
using UnityEngine.SceneManagement;

namespace Raldi
{
    public class RaldiManager : MonoBehaviour
    {
        public Plugin plugin;

        public void Update()
        {
            if (!SceneManager.GetActiveScene().name.Contains("Game") && plugin.loopAudio != null && plugin.audMan != null)
            {
                plugin.ResetAudio();
            }
        }
    }
}
