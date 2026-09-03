using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager))]
public class NetworkSessionController : MonoBehaviour
{
    public static NetworkSessionController Instance { get; private set; }

    private NetworkManager networkManager;
    private ISession activeSession;
    private Task<bool> initializationTask;

    private bool sessionOperationInProgress;
    private bool closeInProgress;

    private string pendingCloseSceneName;

    public bool IsServicesReady { get; private set; }

    public bool IsSessionOperationInProgress => sessionOperationInProgress;

    public bool HasOnlineSession => activeSession != null;

    public string CurrentJoinCode => activeSession?.Code ?? string.Empty;

    public string CurrentSessionId => activeSession?.Id ?? string.Empty;

    public string AuthenticatedPlayerId
    {
        get
        {
            if (!IsServicesReady)
                return string.Empty;

            if (!AuthenticationService.Instance.IsSignedIn)
                return string.Empty;

            return AuthenticationService.Instance.PlayerId;
        }
    }

    public bool TryGetOnlineOpponentPlayerId(out string opponentPlayerId)
    {
        opponentPlayerId = null;

        if (activeSession == null)
            return false;

        string localPlayerId = AuthenticatedPlayerId;

        if (string.IsNullOrWhiteSpace(localPlayerId))
        {
            return false;
        }

        string foundOpponent = null;

        foreach (var player in activeSession.Players)
        {
            if (player == null)
                continue;

            if (string.IsNullOrWhiteSpace(player.Id))
            {
                continue;
            }

            if (string.Equals(player.Id, localPlayerId, StringComparison.Ordinal))
            {
                continue;
            }

            if (foundOpponent != null)
            {
                // Pong Rivals expects exactly one opponent.
                return false;
            }

            foundOpponent = player.Id;
        }

        opponentPlayerId = foundOpponent;

        return !string.IsNullOrWhiteSpace(opponentPlayerId);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        networkManager = GetComponent<NetworkManager>();

        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private async void Start()
    {
        await EnsureServicesReadyAsync();
    }

    private void OnDestroy()
    {
        UnsubscribeFromSceneEvents();

        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public Task<bool> EnsureServicesReadyAsync()
    {
        if (IsServicesReady && AuthenticationService.Instance.IsSignedIn)
        {
            return Task.FromResult(true);
        }

        if (initializationTask == null || (initializationTask.IsCompleted && !IsServicesReady))
        {
            initializationTask = InitializeServicesInternalAsync();
        }

        return initializationTask;
    }

    private async Task<bool> InitializeServicesInternalAsync()
    {
        try
        {
            Debug.Log("[UGS] Initializing Unity Gaming Services.");

            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            IsServicesReady = true;

            Debug.Log($"[UGS] Authentication succeeded. Player ID: {AuthenticationService.Instance.PlayerId}");

            return true;
        }
        catch (Exception exception)
        {
            IsServicesReady = false;

            Debug.LogError("[UGS] Initialization/authentication failed.");
            Debug.LogException(exception);

            return false;
        }
    }

    public async Task<string> CreateRelaySessionAsync()
    {
        if (!CanBeginSessionOperation())
            return null;

        if (!await EnsureServicesReadyAsync())
            return null;

        sessionOperationInProgress = true;

        try
        {
            Debug.Log("[UGS] Creating two-player Relay session.");

            SessionOptions options = new SessionOptions
            {
                MaxPlayers = 2
            }.WithRelayNetwork();

            activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);

            string joinCode = activeSession.Code;

            Debug.Log($"[UGS] Relay session created. Session ID: {activeSession.Id}. Join code: {joinCode}");

            return joinCode;
        }
        catch (SessionException exception)
        {
            activeSession = null;

            Debug.LogError("[UGS] Session creation failed.");
            Debug.LogException(exception);

            return null;
        }
        catch (Exception exception)
        {
            activeSession = null;

            Debug.LogError("[UGS] Unexpected session creation failure.");
            Debug.LogException(exception);

            return null;
        }
        finally
        {
            sessionOperationInProgress = false;
        }
    }

    public async Task<bool> JoinRelaySessionByCodeAsync(string joinCode)
    {
        if (!CanBeginSessionOperation())
            return false;

        string cleanedCode = joinCode?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(cleanedCode))
        {
            Debug.LogWarning("[UGS] A join code is required.");
            return false;
        }

        if (!await EnsureServicesReadyAsync())
            return false;

        sessionOperationInProgress = true;

        try
        {
            Debug.Log($"[UGS] Joining Relay session with code {cleanedCode}.");

            activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(cleanedCode);

            Debug.Log($"[UGS] Relay session joined. Session ID: {activeSession.Id}.");

            return true;
        }
        catch (SessionException exception)
        {
            activeSession = null;

            Debug.LogError("[UGS] Could not join Relay session.");
            Debug.LogException(exception);

            return false;
        }
        catch (Exception exception)
        {
            activeSession = null;

            Debug.LogError("[UGS] Unexpected session join failure.");
            Debug.LogException(exception);

            return false;
        }
        finally
        {
            sessionOperationInProgress = false;
        }
    }

    public async Task<bool> ShutdownCurrentSessionAsync()
    {
        if (sessionOperationInProgress)
            return false;

        sessionOperationInProgress = true;

        try
        {
            await CloseBackendSessionAsync();

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }

            return true;
        }
        finally
        {
            sessionOperationInProgress = false;
        }
    }

    public bool TryCloseSessionToScene(string sceneName)
    {
        if (closeInProgress)
        {
            return false;
        }

        if (networkManager == null)
        {
            Debug.LogError("[NetworkSession] No NetworkManager exists.");
            return false;
        }

        if (!networkManager.IsListening)
        {
            Debug.LogWarning("[NetworkSession] Networking is not active.");
            return false;
        }

        if (!networkManager.IsServer)
        {
            Debug.LogWarning("[NetworkSession] Only the server may close the network session.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[NetworkSession] No return scene was provided.");
            return false;
        }

        closeInProgress = true;
        pendingCloseSceneName = sceneName;

        networkManager.SceneManager.OnSceneEvent += HandleSceneEvent;

        SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"[NetworkSession] Could not begin synchronized load of '{sceneName}'. Status: {status}");

            UnsubscribeFromSceneEvents();

            closeInProgress = false;
            pendingCloseSceneName = null;

            return false;
        }

        Debug.Log($"[NetworkSession] Closing session. Returning all players to '{sceneName}'.");

        return true;
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (!closeInProgress)
            return;

        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        if (sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted)
        {
            return;
        }

        if (sceneEvent.SceneName != pendingCloseSceneName)
        {
            return;
        }

        if (sceneEvent.ClientsThatTimedOut != null && sceneEvent.ClientsThatTimedOut.Count > 0)
        {
            Debug.LogWarning("[NetworkSession] One or more clients timed out while returning to Menus.");
        }

        UnsubscribeFromSceneEvents();

        StartCoroutine(FinalizeCloseAfterSceneLoad());
    }

    private IEnumerator FinalizeCloseAfterSceneLoad()
    {
        yield return null;

        _ = FinalizeCloseAfterSceneLoadAsync();
    }

    private async Task FinalizeCloseAfterSceneLoadAsync()
    {
        sessionOperationInProgress = true;

        try
        {
            await CloseBackendSessionAsync();

            if (networkManager != null && networkManager.IsListening)
            {
                networkManager.Shutdown();
            }
        }
        finally
        {
            sessionOperationInProgress = false;
            closeInProgress = false;
            pendingCloseSceneName = null;
        }
    }

    private async Task CloseBackendSessionAsync()
    {
        if (activeSession == null)
            return;

        ISession sessionToClose = activeSession;

        activeSession = null;

        try
        {
            if (sessionToClose.IsHost)
            {
                Debug.Log("[UGS] Deleting hosted session.");

                await sessionToClose.AsHost().DeleteAsync();

                Debug.Log("[UGS] Hosted session deleted.");
            }
            else
            {
                Debug.Log("[UGS] Leaving joined session.");

                await sessionToClose.LeaveAsync();

                Debug.Log("[UGS] Session left.");
            }
        }
        catch (SessionException exception)
        {
            Debug.LogWarning("[UGS] Backend session cleanup reported an error.");
            Debug.LogException(exception);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[UGS] Unexpected backend session cleanup error.");
            Debug.LogException(exception);
        }
    }

    private bool CanBeginSessionOperation()
    {
        if (sessionOperationInProgress)
        {
            Debug.LogWarning("[UGS] Another session operation is already in progress.");
            return false;
        }

        if (activeSession != null)
        {
            Debug.LogWarning("[UGS] This player is already in a session.");
            return false;
        }

        if (networkManager == null)
        {
            Debug.LogError("[UGS] No NetworkManager exists.");
            return false;
        }

        if (networkManager.IsListening)
        {
            Debug.LogWarning("[UGS] NGO is already running.");
            return false;
        }

        return true;
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (networkManager == null)
            return;

        if (clientId == networkManager.LocalClientId)
        {
            activeSession = null;
        }
    }

    private void UnsubscribeFromSceneEvents()
    {
        if (networkManager == null || networkManager.SceneManager == null)
        {
            return;
        }

        networkManager.SceneManager.OnSceneEvent -= HandleSceneEvent;
    }
}