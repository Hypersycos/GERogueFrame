using Hypersycos.SaveSystem;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class EscMenuScript : MonoBehaviour
    {
        [SerializeField] TypedRegisteredValueSO<string> overrides;

        static EscMenuScript singleton;

        private void Awake()
        {
            if (singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            ControlsWrapper.Singleton.MenuOpened += () => gameObject.SetActive(true);
            ControlsWrapper.Singleton.MenuClosed += () => gameObject.SetActive(false);
            singleton = this;
            gameObject.SetActive(false);
            if (transform.parent.GetChild(1).gameObject.activeSelf)
            {
                SaveSettings();
                transform.parent.GetChild(1).gameObject.SetActive(false);
            }
        }

        public void QuitToLobby()
        {
            if (PersistentStateManager.Singleton != null && PersistentStateManager.Singleton.gameState == GameState.Playing && PersistentStateManager.Singleton.IsServer)
            {
                PersistentStateManager.Singleton.EndGame(GameEndReason.Quit);
            }
        }

        public void QuitToMenu()
        {
            if (PersistentStateManager.Singleton != null)
            {
                PersistentStateManager.SingletonQuitToMenu();
            }
        }

        public void QuitToDesktop()
        {
            Application.Quit();
        }

        public void SaveSettings()
        {
            overrides.Value = ControlsWrapper.Singleton.controls.SaveBindingOverridesAsJson();
            SaveSystem.SaveSystem.Save();
        }
    }
}
