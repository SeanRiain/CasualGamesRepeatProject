using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkConnectionTestUI : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField]
    private Button startHostButton;

    [SerializeField]
    private Button startClientButton;

    [SerializeField]
    private Button shutdownButton;

    [Header("Status")]
    [SerializeField]
    private TMP_Text statusText;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("[NetworkTest] No NetworkManager exists.");

            SetStatus("ERROR: No NetworkManager");

            return;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;

        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        RefreshStatus();
    }

    private void Update()
    {
        RefreshStatus();
    }

    private void OnDestroy()
    {
        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -= HandleClientConnected;

        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }

    public void StartHost()
    {
        if (!CanStartNetwork())
            return;

        bool started = networkManager.StartHost();

        Debug.Log($"[NetworkTest] StartHost returned {started}.");

        RefreshStatus();
    }

    public void StartClient()
    {
        if (!CanStartNetwork())
            return;

        bool started = networkManager.StartClient();

        Debug.Log($"[NetworkTest] StartClient returned {started}.");

        RefreshStatus();
    }

    public void Shutdown()
    {
        if (networkManager == null)
            return;

        if (!networkManager.IsListening)
            return;

        Debug.Log("[NetworkTest] Shutdown requested.");

        networkManager.Shutdown();

        RefreshStatus();
    }

    private bool CanStartNetwork()
    {
        return networkManager != null && !networkManager.IsListening;
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkTest] Client connected: {clientId}. Connected IDs: {GetConnectedClientIds()}");

        RefreshStatus();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkTest] Client disconnected: {clientId}. Connected IDs: {GetConnectedClientIds()}");

        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (networkManager == null)
        {
            SetButtonStates(false, false, false);

            return;
        }

        if (!networkManager.IsListening)
        {
            SetStatus("Network: OFFLINE");

            SetButtonStates(startHost: true, startClient: true, shutdown: false);

            return;
        }

        string role;

        if (networkManager.IsHost)
        {
            role = "HOST";
        }
        else if (networkManager.IsServer)
        {
            role = "SERVER";
        }
        else
        {
            role = "CLIENT";
        }

        string connectionState = networkManager.IsConnectedClient ? "CONNECTED" : "CONNECTING";

        SetStatus($"Role: {role}\n State: {connectionState}\n Local Client ID: {networkManager.LocalClientId}\n Connected IDs: {GetConnectedClientIds()}");

        SetButtonStates(
            startHost: false,
            startClient: false,
            shutdown: true);
    }

    private string GetConnectedClientIds()
    {
        if (networkManager == null)
            return "None";

        if (networkManager.ConnectedClientsIds.Count == 0)
            return "None";

        return string.Join(", ", networkManager.ConnectedClientsIds);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void SetButtonStates(bool startHost, bool startClient, bool shutdown)
    {
        if (startHostButton != null)
        {
            startHostButton.interactable = startHost;
        }

        if (startClientButton != null)
        {
            startClientButton.interactable = startClient;
        }

        if (shutdownButton != null)
        {
            shutdownButton.interactable = shutdown;
        }
    }
}