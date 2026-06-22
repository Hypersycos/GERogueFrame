using System;
using System.Collections.Generic;
using System.Text;
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
            controls.Menu.Enable();
            controls.Menu.CloseMenu.performed += CloseMenu;
            controls.Player.OpenMenu.performed += OpenMenu;
        }

        [RuntimeInitializeOnLoadMethod]
        public static void CreatePlayer1()
        {
            Singleton = new ControlsWrapper();
#if UNITY_EDITOR
            Application.quitting += () => Singleton = null;
#endif
        }

        public void OpenMenu(InputAction.CallbackContext context)
        {
            if (PersistentStateManager.Singleton != null && PersistentStateManager.Singleton.gameState == GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.None;
                controls.Player.Disable();
                controls.Menu.Enable();
                MenuOpened?.Invoke();
            }
        }

        public void CloseMenu(InputAction.CallbackContext context)
        {
            if (PersistentStateManager.Singleton != null && PersistentStateManager.Singleton.gameState == GameState.Playing)
            {
                Cursor.lockState = CursorLockMode.Locked;
                controls.Player.Enable();
                controls.Menu.Disable();
                MenuClosed?.Invoke();
            }
        }
    }
}
