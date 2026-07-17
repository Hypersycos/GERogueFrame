using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hypersycos.GERogueFrame
{
    class ControlsWrapper
    {
        public static ControlsWrapper Singleton;

        public Controls controls { get; private set; }

        public event Action MenuOpened;
        public event Action MenuClosed;

        public ControlsWrapper()
        {
            controls = new();
            controls.MenuScreen.Enable();
            controls.PauseMenu.CloseMenu.performed += CloseMenu;
            controls.Player.OpenMenu.performed += OpenMenu;
            controls.MenuScreen.OpenMenu.performed += OpenMenu;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CreatePlayer1()
        {
            Singleton = new ControlsWrapper();
#if UNITY_EDITOR
            Application.quitting += () => Singleton = null;
#endif
            Singleton.controls.LoadBindingOverridesFromJson(SaveSystem.SaveSystem.Get<string>("Overrides"));
        }

        public void SetUIState(bool state)
        {
            if (state)
            {
                Cursor.lockState = CursorLockMode.None;
                controls.Player.Disable();
                if (PersistentStateManager.Singleton == null || PersistentStateManager.Singleton.gameState == GameState.Lobby)
                {
                    controls.MenuScreen.Enable();
                    controls.PauseMenu.Disable();
                }
                else
                {
                    controls.MenuScreen.Disable();
                    controls.PauseMenu.Enable();
                }
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                controls.Player.Enable();
                controls.PauseMenu.Disable();
                controls.MenuScreen.Disable();
            }
        }

        public void OpenMenu(InputAction.CallbackContext context)
        {
            SetUIState(true);
            MenuOpened?.Invoke();
        }

        public void CloseMenu(InputAction.CallbackContext context)
        {
            if (PersistentStateManager.Singleton != null && PersistentStateManager.Singleton.gameState == GameState.Playing)
            {
                SetUIState(false);
            }
            MenuClosed?.Invoke();
        }

        public void ApplyOverrides(string overrides)
        {
            controls.LoadBindingOverridesFromJson(overrides);
        }

        public string SerializeOverrides()
        {
            return controls.SaveBindingOverridesAsJson();
        }
    }
}
