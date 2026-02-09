using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using CrocoType.Networking;

public class ButtonController : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button testGenerateSentenceBtn;
    
    [SerializeField] private GameSyncManager gameSyncManager;

    private void Awake()
    {
        serverBtn.onClick.AddListener(StartServer);
        hostBtn.onClick.AddListener(StartHost);
        clientBtn.onClick.AddListener(StartClient);
    }

    private void StartServer()
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

        if (NetworkManager.Singleton.StartServer())
        {
            Debug.Log("Server started successfully!");
        }
        else
        {
            Debug.LogError("Failed to start server!");
        }
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

    private void TestGenerateSentence()
    {
        if (gameSyncManager != null)
        {
            gameSyncManager.TestStartRound("This is a test sentence.");
        }
        else
        {
            Debug.LogError("GameSyncManager is not assigned! Cannot generate sentence.");
        }
    }
}
