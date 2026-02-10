using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using CrocoType.Networking;

public class PlayerIconSelection : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Image previewImage;
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private GameSyncManager gameSyncManager;
    
    [Header("Preset Icons")]
    [SerializeField] private Sprite[] presetIcons = new Sprite[4];
    [SerializeField] private Button[] iconButtons = new Button[4];
    [SerializeField] private Image[] iconButtonImages = new Image[4];

    private bool isHost = false;
    private Sprite selectedIcon = null;
    private byte[] iconBytes = null;
    private int selectedIconIndex = -1;

    private void Start()
    {
        // Set up icon selection buttons
        for (int i = 0; i < iconButtons.Length && i < presetIcons.Length; i++)
        {
            int index = i; // Capture for closure
            if (iconButtons[i] != null)
            {
                iconButtons[i].onClick.AddListener(() => OnIconButtonClicked(index));
            }
            
            // Set the icon image on the button
            if (iconButtonImages[i] != null && presetIcons[i] != null)
            {
                iconButtonImages[i].sprite = presetIcons[i];
            }
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }
    }

    public void Initialize(bool host, MainMenu menu)
    {
        isHost = host;
        mainMenu = menu;
        selectedIcon = null;
        iconBytes = null;
        selectedIconIndex = -1;
        
        if (previewImage != null)
            previewImage.sprite = null;
        
        if (confirmButton != null)
            confirmButton.interactable = false;
        
        // Reset button visual states
        UpdateButtonVisualStates();
    }
    
    private void OnIconButtonClicked(int iconIndex)
    {
        if (iconIndex < 0 || iconIndex >= presetIcons.Length || presetIcons[iconIndex] == null)
        {
            Debug.LogWarning($"Invalid icon index: {iconIndex}");
            return;
        }
        
        selectedIconIndex = iconIndex;
        selectedIcon = presetIcons[iconIndex];
        
        // Convert sprite to byte array for network transmission
        iconBytes = SpriteToByteArray(selectedIcon);
        
        // Update preview
        if (previewImage != null)
        {
            previewImage.sprite = selectedIcon;
        }
        
        // Update button visual states
        UpdateButtonVisualStates();
        
        if (confirmButton != null)
            confirmButton.interactable = true;
        
    }
    
    private void UpdateButtonVisualStates()
    {
        // Highlight the selected button, dim others
        for (int i = 0; i < iconButtons.Length; i++)
        {
            if (iconButtons[i] != null)
            {
                // You can add visual feedback here, like changing button colors
                // For now, we'll just ensure the buttons are interactable
                iconButtons[i].interactable = true;
            }
        }
    }
    
    private byte[] SpriteToByteArray(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return null;
        
        // Create a readable copy of the texture
        Texture2D originalTexture = sprite.texture;
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);
        
        Graphics.Blit(originalTexture, renderTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTexture;
        
        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();
        
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTexture);
        
        // Encode the readable texture to PNG bytes
        byte[] pngBytes = readableTexture.EncodeToPNG();
        
        // Clean up
        Destroy(readableTexture);
        
        return pngBytes;
    }


    private void OnConfirmClicked()
    {
        Debug.Log("PlayerIconSelection: Confirm button clicked!");
        
        if (selectedIcon == null || iconBytes == null)
        {
            Debug.LogWarning("No icon selected!");
            return;
        }

        // Verify network is already running (should be started via Relay before reaching this screen)
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null! Network should already be started via Relay.");
            return;
        }

        // Check if network is actually running
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogError("Network is not running! Should be started via Relay before icon selection.");
            return;
        }

        // Store the icon for this player
        if (PlayerIconManager.Instance != null)
        {
            PlayerIconManager.Instance.SetLocalPlayerIcon(iconBytes);
        }
        else
        {
            Debug.LogWarning("PlayerIconManager.Instance is null!");
        }

        Debug.Log($"PlayerIconSelection: Network already running as {(isHost ? "host" : "client")}, sending icon");
        
        // Try to find GameSyncManager if not assigned
        if (gameSyncManager == null)
        {
            gameSyncManager = FindObjectOfType<GameSyncManager>();
            if (gameSyncManager == null)
            {
                Debug.LogWarning("PlayerIconSelection: gameSyncManager not found, will try to find it in coroutine");
            }
        }
        
        // Always start the coroutine - it will handle finding GameSyncManager and has a timeout
        StartCoroutine(SendIconAndProceedToGameplay());
    }

    private System.Collections.IEnumerator SendIconAndProceedToGameplay()
    {
        Debug.Log("PlayerIconSelection: Starting SendIconAndProceedToGameplay coroutine");
        
        // Try to find GameSyncManager if not already found
        float timeout = 10f; // 10 second timeout
        float elapsed = 0f;
        
        while (gameSyncManager == null && elapsed < timeout)
        {
            gameSyncManager = FindObjectOfType<GameSyncManager>();
            if (gameSyncManager == null)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }
        
        // Wait for GameSyncManager to be spawned (if found)
        if (gameSyncManager != null)
        {
            elapsed = 0f;
            while (!gameSyncManager.IsSpawned && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            
            if (!gameSyncManager.IsSpawned)
            {
                Debug.LogWarning("PlayerIconSelection: GameSyncManager not spawned after timeout, proceeding anyway");
            }
        }
        else
        {
            Debug.LogWarning("PlayerIconSelection: GameSyncManager not found after timeout, proceeding without it");
        }

        // Wait a bit for network to be fully ready
        yield return new WaitForSeconds(0.5f);

        // Get local client ID
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        
        Debug.Log($"PlayerIconSelection: Preparing to send icon for client {localClientId}, iconBytes length: {(iconBytes != null ? iconBytes.Length : 0)}");
        
        // Set local player's icon directly 
        if (PlayerIconManager.Instance != null && iconBytes != null)
        {
            PlayerIconManager.Instance.SetPlayerIcon(localClientId, iconBytes);
            Debug.Log($"Set local player icon for client ID: {localClientId}");
        }

        // Send icon via RPC to sync with other clients (if GameSyncManager is available)
        if (gameSyncManager != null && iconBytes != null && gameSyncManager.IsSpawned)
        {
            try
            {
                Debug.Log($"PlayerIconSelection: Sending icon via RPC for client {localClientId}");
                gameSyncManager.SetPlayerIconServerRpc(iconBytes);
                Debug.Log($"Sent icon via RPC for client ID: {localClientId}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"PlayerIconSelection: Error sending icon via RPC: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("PlayerIconSelection: Skipping RPC send - gameSyncManager is null, not spawned, or iconBytes is null");
        }

        // Always show gameplay screen, even if GameSyncManager wasn't found
        Debug.Log("PlayerIconSelection: Proceeding to gameplay screen");
        if (mainMenu != null)
        {
            mainMenu.OnIconSelected();
        }
        else
        {
            Debug.LogError("PlayerIconSelection: mainMenu is null! Cannot proceed to gameplay screen!");
        }
    }
}