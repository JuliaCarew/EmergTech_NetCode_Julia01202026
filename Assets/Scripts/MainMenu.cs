using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private PlayerIconSelection iconSelectionScreen;
    [SerializeField] private GameObject gameplayScreen; 

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
        ShowIconSelection();
    }

    private void OnJoinGameClicked()
    {
        isHost = false;
        ShowIconSelection();
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