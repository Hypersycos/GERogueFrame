using UnityEngine;

namespace Hypersycos.Utils
{
    public class KeepMe : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}