using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CrocoType.Networking;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject loseScreen;
    [SerializeField] private GameObject winScreen;
    
    private GameSyncManager _syncManager;
    private MainMenu _mainMenu;

    void Start()
    {
        // Find GameSyncManager
        _syncManager = FindObjectOfType<GameSyncManager>();
        
        if (_syncManager == null)
        {
            Debug.LogError("GameOverUI: GameSyncManager not found!");
            return;
        }

        // Find MainMenu
        _mainMenu = FindObjectOfType<MainMenu>();

        // Subscribe to events
        _syncManager.OnPlayerEliminated += OnPlayerEliminated;
        _syncManager.OnGameOver += OnGameOver;
        _syncManager.OnPhaseChanged += OnPhaseChanged;

        // Initially hide all screens
        if (loseScreen != null)
            loseScreen.SetActive(false);
        if (winScreen != null)
            winScreen.SetActive(false);
    }

    void OnDestroy()
    {
        if (_syncManager != null)
        {
            _syncManager.OnPlayerEliminated -= OnPlayerEliminated;
            _syncManager.OnGameOver -= OnGameOver;
            _syncManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnPlayerEliminated(ulong eliminatedClientId, int lethalToothIndex)
    {
        // Check if the eliminated player is the local player
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == eliminatedClientId)
        {
            Debug.Log($"GameOverUI: Local player eliminated! They selected the death tooth (index {lethalToothIndex})");
            ShowLoseScreen();
        }
    }

    private void OnGameOver(ulong winnerClientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        if (localClientId == winnerClientId)
        {
            Debug.Log($"GameOverUI: Local player won! (Client {winnerClientId})");
            ShowWinScreen();
        }
        else
        {
            Debug.Log($"GameOverUI: Game Over - Winner is client {winnerClientId}, local player lost");
            
            if (loseScreen != null && !loseScreen.activeSelf)
            {
                ShowLoseScreen();
            }
        }
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        // Hide screens when starting a new game phase except GameOver
        if (phase != GamePhase.GameOver)
        {
            if (loseScreen != null)
                loseScreen.SetActive(false);
            if (winScreen != null)
                winScreen.SetActive(false);
        }
    }

    private void ShowLoseScreen()
    {
        if (loseScreen != null)
        {
            loseScreen.SetActive(true);
            Debug.Log("GameOverUI: Showing Lose Screen");
        }
        
        // Hide win screen if it's showing
        if (winScreen != null)
            winScreen.SetActive(false);
    }

    private void ShowWinScreen()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(true);
            Debug.Log("GameOverUI: Showing Win Screen");
        }
        
        // Hide lose screen if it's showing
        if (loseScreen != null)
            loseScreen.SetActive(false);
    }

    // disconnect from the network and show the main menu screen
    public void GoToMainMenu()
    {
        Debug.Log("GameOverUI: Returning to main menu...");

        // Hide win/lose screens
        if (loseScreen != null)
            loseScreen.SetActive(false);
        if (winScreen != null)
            winScreen.SetActive(false);

        // Shutdown network connection
        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
        {
            Debug.Log("GameOverUI: Shutting down network connection...");
            NetworkManager.Singleton.Shutdown();
        }

        // Show main menu
        if (_mainMenu != null)
        {
            _mainMenu.ShowMainMenu();
        }
    }
}

