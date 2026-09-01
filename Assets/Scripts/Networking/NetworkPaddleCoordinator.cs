using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPaddleCoordinator : NetworkBehaviour
{
    [Header("Scene Paddles")]
    [SerializeField] private NetworkObject leftPaddle;
    [SerializeField] private NetworkObject rightPaddle;

    public NetworkVariable<ulong> LeftClientId = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> RightClientId = new NetworkVariable<ulong>(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Coroutine assignmentRoutine;

    public override void OnNetworkSpawn()
    {
        LeftClientId.OnValueChanged += HandleLeftClientChanged;
        RightClientId.OnValueChanged += HandleRightClientChanged;

        LogAssignments();

        if (!IsServer)
            return;

        NetworkManager.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        StartAssignmentAttempt();
    }

    public override void OnNetworkDespawn()
    {
        LeftClientId.OnValueChanged -= HandleLeftClientChanged;
        RightClientId.OnValueChanged -= HandleRightClientChanged;

        if (IsServer && NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        if (assignmentRoutine != null)
        {
            StopCoroutine(assignmentRoutine);
            assignmentRoutine = null;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        StartAssignmentAttempt();
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (!IsServer)
            return;

        if (RightClientId.Value == clientId)
        {
            RightClientId.Value = ulong.MaxValue;
        }

        if (LeftClientId.Value == clientId)
        {
            LeftClientId.Value = ulong.MaxValue;
        }

        StartAssignmentAttempt();
    }

    private void StartAssignmentAttempt()
    {
        if (!IsServer)
            return;

        if (assignmentRoutine != null)
        {
            StopCoroutine(assignmentRoutine);
        }

        assignmentRoutine = StartCoroutine(AssignPaddlesWhenReady());
    }

    private IEnumerator AssignPaddlesWhenReady()
    {
        const float timeoutSeconds = 5f;

        float timeoutAt = Time.realtimeSinceStartup + timeoutSeconds;

        while (Time.realtimeSinceStartup < timeoutAt)
        {
            if (PaddlesAreReady() && TryGetRemoteClientId(out ulong remoteClientId))
            {
                AssignPaddles(remoteClientId);

                assignmentRoutine = null;
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning($"[NetworkPaddleCoordinator] Could not assign both paddles within {timeoutSeconds} seconds.");

        assignmentRoutine = null;
    }

    private bool PaddlesAreReady()
    {
        return leftPaddle != null && rightPaddle != null && leftPaddle.IsSpawned && rightPaddle.IsSpawned;
    }

    private bool TryGetRemoteClientId(out ulong remoteClientId)
    {
        remoteClientId = ulong.MaxValue;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                continue;
            }

            remoteClientId = clientId;
            return true;
        }

        return false;
    }

    private void AssignPaddles(ulong remoteClientId)
    {
        ulong hostClientId = NetworkManager.ServerClientId;

        LeftClientId.Value = hostClientId;
        RightClientId.Value = remoteClientId;

        if (leftPaddle.OwnerClientId != hostClientId)
        {
            leftPaddle.ChangeOwnership(hostClientId);
        }

        if (rightPaddle.OwnerClientId != remoteClientId)
        {
            rightPaddle.ChangeOwnership(remoteClientId);
        }

        Debug.Log($"[NetworkPaddleCoordinator] Left -> Client {hostClientId}, Right -? Client {remoteClientId}.");
    }

    private void HandleLeftClientChanged(ulong previousValue, ulong newValue)
    {
        LogAssignments();
    }

    private void HandleRightClientChanged(ulong previousValue, ulong newValue)
    {
        LogAssignments();
    }

    private void LogAssignments()
    {
        string left = LeftClientId.Value == ulong.MaxValue ? "Unassigned" : LeftClientId.Value.ToString();
        string right = RightClientId.Value == ulong.MaxValue ? "Unassigned" : RightClientId.Value.ToString();

        Debug.Log($"[NetworkPaddleCoordinator] Local Client {NetworkManager.LocalClientId}: Left={left}, Right={right}.");
    }
}