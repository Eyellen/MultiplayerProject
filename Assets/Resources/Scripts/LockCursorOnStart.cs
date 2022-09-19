using UnityEngine;

namespace GameEngine
{
    public class LockCursorOnStart : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
    }
}
