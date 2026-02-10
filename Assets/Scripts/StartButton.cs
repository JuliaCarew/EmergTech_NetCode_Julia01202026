using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CrocoType.States;

public class StartButton : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private SentenceGenerator sentenceGenerator;
    [SerializeField] private GameStateMachine stateMachine;

    private void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
            UpdateButtonVisibility();
        }

        // Subscribe to NetworkManager events for better visibility updates
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void OnServerStarted()
    {
        UpdateButtonVisibility();
    }

    private void Update()
    {
        // Update button visibility based on network status
        if (startButton != null)
        {
            bool shouldBeVisible = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            if (startButton.gameObject.activeSelf != shouldBeVisible)
            {
                startButton.gameObject.SetActive(shouldBeVisible);
            }
        }
    }

    private void UpdateButtonVisibility()
    {
        // Only show the button to the host/server
        if (startButton != null)
        {
            bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
            startButton.gameObject.SetActive(isServer);
        }
    }

    private void OnStartButtonClicked()
    {
        // Only allow the host/server to start the game
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host can start the game!");
            return;
        }

        // Update the sentence in SentenceGenerator 
        if (sentenceGenerator != null)
        {
            sentenceGenerator.UpdateSentence();
        }

        // Get the TypingState and transition to it
        if (stateMachine != null)
        {
            var typingState = stateMachine.GetState<TypingState>();
            if (typingState != null)
            {
                stateMachine.SetState(typingState);
            }
        }
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }

        // Unsubscribe from NetworkManager events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}

