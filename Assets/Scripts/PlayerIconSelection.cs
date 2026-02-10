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
        if (selectedIcon == null || iconBytes == null)
        {
            Debug.LogWarning("No icon selected!");
            return;
        }

        // Store the icon for this player
        PlayerIconManager.Instance.SetLocalPlayerIcon(iconBytes);

        // Start network connection
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton is null!");
            return;
        }

        // Shutdown any existing connection
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }

        bool success = false;
        if (isHost)
        {
            success = NetworkManager.Singleton.StartHost();
            if (success)
                Debug.Log("Host started with icon!");
        }
        else
        {
            success = NetworkManager.Singleton.StartClient();
            if (success)
                Debug.Log("Client started with icon!");
        }

        if (success)
        {
            Debug.Log($"PlayerIconSelection: Network started successfully as {(isHost ? "host" : "client")}, starting icon send coroutine");
            // Send icon to server
            if (gameSyncManager != null)
            {
                // Wait for network spawn, then send icon
                StartCoroutine(SendIconAfterSpawn());
            }
            else
            {
                Debug.LogWarning("PlayerIconSelection: gameSyncManager is null, cannot send icon");
                // Show gameplay screen anyway
                if (mainMenu != null)
                    mainMenu.OnIconSelected();
            }
        }
    }

    private System.Collections.IEnumerator SendIconAfterSpawn()
    {
        // Wait for GameSyncManager to be spawned
        while (gameSyncManager == null || !gameSyncManager.IsSpawned)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Wait for network to be fully ready
        yield return new WaitForSeconds(0.5f);

        // Get local client ID
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        Debug.Log($"PlayerIconSelection: Preparing to send icon for client {localClientId}, iconBytes length: {(iconBytes != null ? iconBytes.Length : 0)}");
        
        // Set local player's icon directly 
        if (PlayerIconManager.Instance != null && iconBytes != null)
        {
            PlayerIconManager.Instance.SetPlayerIcon(localClientId, iconBytes);
            Debug.Log($"Set local player icon for client ID: {localClientId}");
        }

        // Send icon via RPC to sync with other clients
        if (gameSyncManager != null && iconBytes != null)
        {
            Debug.Log($"PlayerIconSelection: Sending icon via RPC for client {localClientId}");
            gameSyncManager.SetPlayerIconServerRpc(iconBytes);
            Debug.Log($"Sent icon via RPC for client ID: {localClientId}");
        }

        // Show gameplay screen
        if (mainMenu != null)
            mainMenu.OnIconSelected();
    }
}