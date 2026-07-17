using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuBehaviour : MonoBehaviour
    {
        [SerializeField] NetworkManager networkManager;
        [SerializeField] UnityTransport networkTransport;
        [SerializeField] UIDocument document;

        private void Reset()
        {
            document = GetComponent<UIDocument>();
            networkManager = NetworkManager.Singleton;
            if (networkManager != null)
                networkTransport = networkManager.GetComponent<UnityTransport>();
        }

        string networkAddress { get => networkTransport.ConnectionData.Address; set => networkTransport.ConnectionData.Address = value; }

        private Button play;
        private Button builds;
        private Button settings;
        private Button quit;

        private VisualElement mainMenuContainer;
        private VisualElement JoinOrHost;

        private Button back;
        private Button host;
        private TextField ip;
        string ipValue { get => ip.text == "" ? "localhost" : ip.text; }
        private Button join;
        private Label error;

        Coroutine errorCoroutine;

        private void OnEnable()
        {
            VisualElement root = document.rootVisualElement;
            play = root.Q<Button>("Play");
            builds = root.Q<Button>("Builds");
            settings = root.Q<Button>("Settings");
            quit = root.Q<Button>("Quit");

            mainMenuContainer = root.Q<VisualElement>("MainMenu");
            JoinOrHost = root.Q<VisualElement>("JoinOrHost");

            back = root.Q<Button>("Backbutton");
            host = root.Q<Button>("Host");
            ip = root.Q<TextField>("IPField");
            join = root.Q<Button>("Join");
            error = root.Q<Label>("ErrorText");

            play.clicked += PlayClicked;
            builds.clicked += BuildsClicked;
            settings.clicked += SettingsClicked;
            quit.clicked += QuitClicked;

            back.clicked += BackClicked;
            host.clicked += HostClicked;
            join.clicked += JoinClicked;

            BackClicked();
            this.error.text = "";
        }

        private void Start()
        {
            ModLoader.RegisterPrefabs();
        }

        private void PlayClicked()
        {
            JoinOrHost.style.display = DisplayStyle.Flex;
            mainMenuContainer.style.display = DisplayStyle.None;
        }

        private void BuildsClicked()
        {

        }

        private void SettingsClicked()
        {
            ControlsWrapper.Singleton.OpenMenu(default);
        }

        private void BackClicked()
        {
            JoinOrHost.style.display = DisplayStyle.None;
            mainMenuContainer.style.display = DisplayStyle.Flex;
        }

        private void HostClicked()
        {
            JoinOrHost.enabledSelf = false;
            if (networkManager.StartHost())
            {
                networkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                JoinOrHost.enabledSelf = true;
                DisplayError("Failed to host");
            }
        }

        private void JoinClicked()
        {
            JoinOrHost.enabledSelf = false;
            networkAddress = ipValue;
            networkTransport.OnTransportEvent += HandleClientEvent;
            if (networkManager.StartClient())
            {
                networkTransport.OnTransportEvent -= HandleClientEvent;
            }
            else
            {
                networkTransport.OnTransportEvent -= HandleClientEvent;
                JoinOrHost.enabledSelf = true;
                DisplayError("Failed to start network");
            }
        }

        private void HandleClientEvent(NetworkEvent eventType, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (eventType == NetworkEvent.Disconnect || eventType == NetworkEvent.TransportFailure)
            {
                networkTransport.OnTransportEvent -= HandleClientEvent;
                JoinOrHost.enabledSelf = true;
                DisplayError("Couldn't connect to " + ipValue);
            }
        }

        private void DisplayError(string error)
        {
            if (errorCoroutine != null)
                StopCoroutine(errorCoroutine);
            StartCoroutine(DisplayErrorCoroutine(error));
        }

        IEnumerator DisplayErrorCoroutine(string error, float duration = 5)
        {
            this.error.text = error;
            yield return new WaitForSecondsRealtime(duration);
            this.error.text = "";
        }

        private void QuitClicked()
        {
            Application.Quit(0);
        }

        private void OnDisable()
        {
            if (play != null) play.clicked -= PlayClicked;
            if (builds != null) builds.clicked -= BuildsClicked;
            if (settings != null) settings.clicked -= SettingsClicked;
            if (quit != null) play.clicked -= QuitClicked;
            if (back != null) host.clicked -= HostClicked;
            if (host != null) host.clicked -= HostClicked;
            if (join != null) join.clicked -= JoinClicked;
        }
    }
}