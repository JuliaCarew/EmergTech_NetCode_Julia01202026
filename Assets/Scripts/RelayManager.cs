using UnityEngine;
using System.Collections;
using TMPro;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;

public class RelayManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TMP_InputField joinCodeInputField;

    public TMP_InputField GetJoinCodeInputField() => joinCodeInputField;
    
    public bool IsJoinCodeReady()
    {
        return joinCodeInputField != null && !string.IsNullOrWhiteSpace(joinCodeInputField.text);
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartRelay()
    {
        try
        {
            string joinCode = await StartHost();

            if (!string.IsNullOrEmpty(joinCode) && joinCodeText != null)
            {
                joinCodeText.text = $"Join Code: {joinCode}";
                Debug.Log($"Relay host started! Join Code: {joinCode}");
            }
            else
            {
                Debug.LogError("Failed to start Relay host or get join code!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error starting Relay host: {e.Message}");
        }
    }

    public async void JoinRelay()
    {
        try
        {
            if (joinCodeInputField == null)
            {
                Debug.LogError("Join code input field is not assigned in RelayManager!");
                return;
            }

            // Get and trim the join code
            string joinCode = joinCodeInputField.text?.Trim();
            
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Join code is empty! Please enter a valid join code.");
                return;
            }

            Debug.Log($"Attempting to join Relay game with join code: '{joinCode}' (length: {joinCode.Length})");
            
            bool success = await StartClient(joinCode);
            
            if (success)
            {
                Debug.Log("Successfully joined Relay game!");
            }
            else
            {
                Debug.LogError("Failed to join Relay game!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error joining Relay game: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    private async Task<string> StartHost(int maxConnections = 3)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return null;
        }

        // Shutdown any existing connection
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Network is already running! Shutting down first...");
            NetworkManager.Singleton.Shutdown();
        }

        Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(maxConnections);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport component not found on NetworkManager!");
            return null;
        }

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        bool hostStarted = NetworkManager.Singleton.StartHost();
        return hostStarted ? joinCode : null;
    }

    private async Task<bool> StartClient(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is null or empty!");
            return false;
        }

        // Trim whitespace
        joinCode = joinCode.Trim();
        
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is empty after trimming!");
            return false;
        }

        Debug.Log($"StartClient: Attempting to join with code '{joinCode}'");

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return false;
        }

        // Shutdown any existing connection
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Network is already running! Shutting down first...");
            NetworkManager.Singleton.Shutdown();
        }

        try
        {
            Debug.Log($"Calling RelayService.JoinAllocationAsync with code: '{joinCode}'");
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("Successfully received JoinAllocation from Relay service");

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component not found on NetworkManager!");
                return false;
            }

            transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));
            Debug.Log("Set relay server data on UnityTransport");

            bool clientStarted = NetworkManager.Singleton.StartClient();
            Debug.Log($"NetworkManager.StartClient() returned: {clientStarted}");
            return clientStarted;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception in StartClient when joining with code '{joinCode}': {e.GetType().Name} - {e.Message}");
            throw; // Re-throw to be caught by JoinRelay
        }
    }
}
