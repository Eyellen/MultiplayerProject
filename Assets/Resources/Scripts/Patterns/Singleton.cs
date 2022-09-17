using UnityEngine;

namespace GameEngine.Patterns
{
    public class Singleton<T> where T : MonoBehaviour
    {
        public T Instance { get; private set; }

        public void Initialize(T obj)
        {
            if (Instance == null)
                Instance = obj;
            else
            {
                Debug.LogError($"Trying to create another {obj} when there is already one in the scene." +
                    $" The duplicate {obj} will be destroyed.");
                GameObject.Destroy(obj.gameObject);
            }
        }
    }
}
