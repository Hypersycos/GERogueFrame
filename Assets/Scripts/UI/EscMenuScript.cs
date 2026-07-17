using Hypersycos.SaveSystem;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    public class EscMenuScript : MonoBehaviour
    {
        [SerializeField] TypedRegisteredValueSO<string> overrides;
        [SerializeField] GameObject settings;
        [SerializeField] GameObject gameMenu;
        [SerializeField] GameObject quitToLobby;

        static EscMenuScript singleton;

        private void Awake()
        {
            if (singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            ControlsWrapper.Singleton.MenuOpened += MenuOpened;
            ControlsWrapper.Singleton.MenuClosed += MenuClosed;
            singleton = this;
            gameObject.SetActive(false);
            if (transform.parent.GetChild(1).gameObject.activeSelf)
            {
                SaveSettings();
                transform.parent.GetChild(1).gameObject.SetActive(false);
            }
        }

        public void MenuOpened()
        {
            gameObject.SetActive(true);
            if (PersistentStateManager.Singleton != null)
            {
                if (PersistentStateManager.Singleton.gameState == GameState.Lobby || !PersistentStateManager.Singleton.IsServer)
                    quitToLobby.SetActive(false);
                else
                    quitToLobby.SetActive(true);
                gameMenu.SetActive(true);
                settings.SetActive(false);
            }
            else
            {
                gameMenu.SetActive(false);
                settings.SetActive(true);
            }
        }

        public void MenuClosed()
        {
            gameObject.SetActive(false);
        }

        public void QuitToLobby()
        {
            if (PersistentStateManager.Singleton != null && PersistentStateManager.Singleton.gameState == GameState.Playing && PersistentStateManager.Singleton.IsServer)
            {
                PersistentStateManager.Singleton.EndGame(GameEndReason.Quit);
                ControlsWrapper.Singleton.CloseMenu(default);
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
