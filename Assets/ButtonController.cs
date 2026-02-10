using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using CrocoType.Networking;

public class ButtonController : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    
    [SerializeField] private GameSyncManager gameSyncManager;

    private void Awake()
    {
        hostBtn.onClick.AddListener(StartHost);
        clientBtn.onClick.AddListener(StartClient);
    }

    private void StartHost()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Network is already running! Shutting down first...");
            NetworkManager.Singleton.Shutdown();
        }

        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("Host started successfully!");
        }
        else
        {
            Debug.LogError("Failed to start host!");
        }
    }

    private void StartClient()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return;
        }

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Network is already running! Shutting down first...");
            NetworkManager.Singleton.Shutdown();
        }

        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("Client started successfully!");
        }
        else
        {
            Debug.LogError("Failed to start client!");
        }
    }
}
