using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkPaddleController : NetworkBehaviour
{
    [Header("Paddle Identity")]
    [SerializeField]
    private PlayerSide side;

    [Header("Local Input")]
    [SerializeField]
    private Slider movementSlider;

    [SerializeField]
    private Behaviour legacyLocalController;

    [Header("Movement")]
    [SerializeField]
    private Rigidbody2D body;

    [SerializeField]
    private float movementSpeed = 8f;

    [SerializeField]
    private float minY = -3.88f;

    [SerializeField]
    private float maxY = 3.90f;

    [Header("Network Input")]
    [SerializeField]
    private float inputSendRate = 30f;

    [SerializeField]
    [Range(0f, 0.1f)]
    private float minimumInputDelta = 0.002f;

    private float serverTargetNormalized = 0.5f;

    private float lastSentInput = float.NaN;
    private float nextInputSendTime;

    private bool wasLocalOwner;

    public PlayerSide Side => side;

    public override void OnNetworkSpawn()
    {
        if (legacyLocalController != null)
        {
            legacyLocalController.enabled = false;
        }

        if (IsServer && body != null)
        {
            serverTargetNormalized = WorldYToNormalized(body.position.y);
        }

        wasLocalOwner = false;

        Debug.Log
            (
            $"[NetworkPaddle] {side} spawned. " +
            $"Owner Client ID: {OwnerClientId}. " +
            $"Local Client ID: {NetworkManager.LocalClientId}. " +
            $"IsOwner: {IsOwner}. " +
            $"IsServer: {IsServer}."
            );
    }

    public override void OnNetworkDespawn()
    {
        if (legacyLocalController != null)
        {
            legacyLocalController.enabled = true;
        }

        wasLocalOwner = false;
        lastSentInput = float.NaN;
    }

    private void Update()
    {
        if (!IsSpawned)
            return;

        bool isLocalOwner = IsOwner;

        if (isLocalOwner && !wasLocalOwner)
        {
            PrepareLocalInput();
        }

        wasLocalOwner = isLocalOwner;

        if (!isLocalOwner || movementSlider == null)
            return;

        if (Time.unscaledTime < nextInputSendTime)
            return;

        float sendInterval = 1f / Mathf.Max(1f, inputSendRate);

        nextInputSendTime = Time.unscaledTime + sendInterval;

        float normalizedTarget = Mathf.Clamp01(movementSlider.value);

        if (!float.IsNaN(lastSentInput) && Mathf.Abs(normalizedTarget - lastSentInput) < minimumInputDelta)
        {
            return;
        }

        SubmitTargetRpc(normalizedTarget);

        lastSentInput = normalizedTarget;
    }

    [Rpc(SendTo.Server, RequireOwnership = true, Delivery = RpcDelivery.Unreliable)]
    private void SubmitTargetRpc(float normalizedTarget)
    {
        serverTargetNormalized = Mathf.Clamp01(normalizedTarget);
    }

    private void FixedUpdate()
    {
        if (!IsSpawned ||
            !IsServer ||
            body == null)
        {
            return;
        }

        float targetY = Mathf.Lerp(minY, maxY, serverTargetNormalized);

        Vector2 currentPosition = body.position;

        float nextY = Mathf.MoveTowards(currentPosition.y, targetY, movementSpeed * Time.fixedDeltaTime);

        body.MovePosition(new Vector2(currentPosition.x, nextY));
    }

    private void PrepareLocalInput()
    {
        if (movementSlider == null || body == null)
        {
            return;
        }

        float normalizedPosition = WorldYToNormalized(body.position.y);

        movementSlider.SetValueWithoutNotify( normalizedPosition);

        lastSentInput = float.NaN;
        nextInputSendTime = 0f;

        Debug.Log($"[NetworkPaddle] Local client {NetworkManager.LocalClientId} now controls {side}.");
    }

    private float WorldYToNormalized(float worldY)
    {
        if (Mathf.Approximately(minY, maxY))
        {
            return 0.5f;
        }

        return Mathf.InverseLerp(minY, maxY, worldY);
    }
}