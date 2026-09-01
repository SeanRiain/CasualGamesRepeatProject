using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkConnectionTestUI : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private Button shutdownButton;
    [SerializeField] private TMP_InputField joinCodeInput;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    private NetworkManager networkManager;
    private NetworkSessionController sessionController;

    private bool uiBusy;
    private string busyMessage;
    private string informationMessage;

    private async void Start()
    {
        networkManager = NetworkManager.Singleton;
        sessionController = NetworkSessionController.Instance;

        if (networkManager == null)
        {
            Debug.LogError("[NetworkTest] No NetworkManager exists.");
            SetStatus("ERROR: No NetworkManager");
            SetButtonStates(false, false, false);
            return;
        }

        if (sessionController == null)
        {
            Debug.LogError("[NetworkTest] No NetworkSessionController exists.");
            SetStatus("ERROR: No Session Controller");
            SetButtonStates(false, false, false);
            return;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        SetBusy("UGS: INITIALIZING...");

        bool ready = await sessionController.EnsureServicesReadyAsync();

        ClearBusy();

        if (!ready)
        {
            informationMessage = "UGS initialization failed. Check Console.";
        }

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

    public async void StartHost()
    {
        if (!CanStartNetwork())
            return;

        informationMessage = string.Empty;

        SetBusy("Creating Relay session...");

        string joinCode = await sessionController.CreateRelaySessionAsync();

        ClearBusy();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            informationMessage = "Could not create Relay session. Check Console.";
        }

        RefreshStatus();
    }

    public async void StartClient()
    {
        if (!CanStartNetwork())
            return;

        string joinCode = joinCodeInput != null ? joinCodeInput.text : string.Empty;

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            informationMessage = "Enter a join code.";
            RefreshStatus();
            return;
        }

        informationMessage = string.Empty;

        SetBusy("Joining Relay session...");

        bool joined = await sessionController.JoinRelaySessionByCodeAsync(joinCode);

        ClearBusy();

        if (!joined)
        {
            informationMessage = "Could not join Relay session. Check code and Console.";
        }

        RefreshStatus();
    }

    public async void Shutdown()
    {
        if (sessionController == null)
            return;

        SetBusy("Closing session...");

        await sessionController.ShutdownCurrentSessionAsync();

        ClearBusy();

        informationMessage = string.Empty;

        RefreshStatus();
    }

    private bool CanStartNetwork()
    {
        return networkManager != null &&
               sessionController != null &&
               !uiBusy &&
               !sessionController.IsSessionOperationInProgress &&
               !networkManager.IsListening &&
               !sessionController.HasOnlineSession;
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
        if (networkManager == null || sessionController == null)
        {
            return;
        }

        if (uiBusy)
        {
            SetStatus(busyMessage);
            SetButtonStates(false, false, false);
            SetJoinCodeInputState(false);
            return;
        }

        if (!sessionController.IsServicesReady)
        {
            SetStatus("UGS: NOT READY\n" + informationMessage);
            SetButtonStates(false, false, false);
            SetJoinCodeInputState(false);
            return;
        }

        string playerId = sessionController.AuthenticatedPlayerId;

        if (!networkManager.IsListening)
        {
            string status = "UGS: READY\n" +
                            $"Player ID: {playerId}\n" +
                            "Network: OFFLINE";

            if (!string.IsNullOrWhiteSpace(informationMessage))
            {
                status += "\n" + informationMessage;
            }

            SetStatus(status);

            bool canStart = !sessionController.HasOnlineSession;

            SetButtonStates(canStart, canStart, sessionController.HasOnlineSession);
            SetJoinCodeInputState(canStart);

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

        string statusTextValue = "UGS: READY\n" +
                                 $"Player ID: {playerId}\n" +
                                 $"Role: {role}\n" +
                                 $"State: {connectionState}\n" +
                                 $"Local Client ID: {networkManager.LocalClientId}\n" +
                                 $"Connected IDs: {GetConnectedClientIds()}";

        if (networkManager.IsHost)
        {
            string joinCode = sessionController.CurrentJoinCode;

            if (!string.IsNullOrWhiteSpace(joinCode))
            {
                statusTextValue += $"\nJoin Code: {joinCode}";
            }
        }

        SetStatus(statusTextValue);

        SetButtonStates(false, false, true);
        SetJoinCodeInputState(false);
    }

    private string GetConnectedClientIds()
    {
        if (networkManager == null)
            return "None";

        if (networkManager.ConnectedClientsIds.Count == 0)
        {
            return "None";
        }

        return string.Join(", ", networkManager.ConnectedClientsIds);
    }

    private void SetBusy(string message)
    {
        uiBusy = true;
        busyMessage = message;

        RefreshStatus();
    }

    private void ClearBusy()
    {
        uiBusy = false;
        busyMessage = string.Empty;
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

    private void SetJoinCodeInputState(bool interactable)
    {
        if (joinCodeInput != null)
        {
            joinCodeInput.interactable = interactable;
        }
    }
}