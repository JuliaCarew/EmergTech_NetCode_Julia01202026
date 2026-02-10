using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private PlayerIconSelection iconSelectionScreen;
    [SerializeField] private GameObject gameplayScreen;
    [SerializeField] private RelayManager relayManager;

    private bool isHost = false;

    private void Start()
    {
        // Show main menu by default
        ShowMainMenu();

        // Set up button listeners
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (joinGameButton != null)
        {
            joinGameButton.onClick.AddListener(OnJoinGameClicked);
        }
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (gameplayScreen != null)
            gameplayScreen.SetActive(false);
        
        if (iconSelectionScreen != null)
            iconSelectionScreen.gameObject.SetActive(false);
    }

    public void ShowGameplayScreen()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (gameplayScreen != null)
            gameplayScreen.SetActive(true);
        
        if (iconSelectionScreen != null)
            iconSelectionScreen.gameObject.SetActive(false);
    }

    private void OnStartGameClicked()
    {
        isHost = true;
        
        // Start Relay host before showing icon selection
        if (relayManager != null)
        {
            relayManager.StartRelay();
            // Wait a moment for network to initialize, then show icon selection
            StartCoroutine(WaitForRelayAndShowIconSelection());
        }
        else
        {
            Debug.LogError("RelayManager is not assigned in MainMenu!");
            ShowIconSelection();
        }
    }
    
    private System.Collections.IEnumerator WaitForRelayAndShowIconSelection()
    {
        // Wait for network to be ready
        yield return new WaitForSeconds(0.5f);
        
        // Check if network is actually running (as host, server, or client)
        while (Unity.Netcode.NetworkManager.Singleton == null || 
               (!Unity.Netcode.NetworkManager.Singleton.IsHost && 
                !Unity.Netcode.NetworkManager.Singleton.IsServer && 
                !Unity.Netcode.NetworkManager.Singleton.IsClient))
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log($"Network is ready! IsHost: {Unity.Netcode.NetworkManager.Singleton.IsHost}, IsServer: {Unity.Netcode.NetworkManager.Singleton.IsServer}, IsClient: {Unity.Netcode.NetworkManager.Singleton.IsClient}");
        ShowIconSelection();
    }

    private void OnJoinGameClicked()
    {
        isHost = false;
        
        // Check if join code is entered before attempting to join
        if (relayManager != null)
        {
            // Check if join code is ready
            if (!relayManager.IsJoinCodeReady())
            {
                Debug.LogWarning("Please enter a join code in the input field before joining!");
                return;
            }
            
            // Start Relay client join before showing icon selection
            relayManager.JoinRelay();
            // Wait for network to initialize, then show icon selection
            StartCoroutine(WaitForRelayAndShowIconSelection());
        }
        else
        {
            Debug.LogError("RelayManager is not assigned in MainMenu!");
            ShowIconSelection();
        }
    }

    private void ShowIconSelection()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (iconSelectionScreen != null)
        {
            iconSelectionScreen.gameObject.SetActive(true);
            iconSelectionScreen.Initialize(isHost, this);
        }
    }

    public void OnIconSelected()
    {
        ShowGameplayScreen();
    }
}