using Unity.Netcode;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class ExperimentManager : MonoBehaviour
    {
        [SerializeField] NetworkManager networkManager;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            ModLoader.RegisterPrefabs();
            networkManager.StartHost();
            networkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
