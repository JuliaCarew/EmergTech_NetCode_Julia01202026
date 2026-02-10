using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using CrocoType.Networking;

public class PlayerIconDisplay : MonoBehaviour
{
    [SerializeField] private Transform iconContainer;
    [SerializeField] private Image[] playerIconImages = new Image[4]; // 4 pre-existing Image objects
    [SerializeField] private GameSyncManager gameSyncManager;

    private Dictionary<ulong, int> playerToSlotMap = new Dictionary<ulong, int>(); // Maps clientId to slot index
    private int nextAvailableSlot = 0;
    public bool debugMode = false;

    private void Start()
    {
        if (gameSyncManager != null)
        {
            gameSyncManager.OnPlayerStatesSync += OnPlayerStatesSync;
        }

        // Subscribe to player icon updates
        if (PlayerIconManager.Instance != null)
        {
            PlayerIconManager.Instance.OnIconSet += OnIconSet;
            // Check for existing players periodically
            InvokeRepeating(nameof(UpdatePlayerIcons), 0.5f, 0.5f);
        }
        
        // Also update when network is ready
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }
    
    private void OnIconSet(ulong clientId)
    {
        if (!playerToSlotMap.ContainsKey(clientId))
        {
            if(debugMode) Debug.Log($"PlayerIconDisplay: Icon set for client {clientId}, assigning slot");
            AssignSlotToPlayer(clientId);
        }
        else
        {
            UpdatePlayerIconUI(clientId);
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        // Update icons when a new client connects
        UpdatePlayerIcons();
    }

    private void OnEnable()
    {
        // Deactivate all icon images initially
        foreach (var iconImage in playerIconImages)
        {
            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(false);
            }
        }
        
        // Reset slot mapping
        playerToSlotMap.Clear();
        nextAvailableSlot = 0;
        
        // Initial update when component is enabled
        if (gameSyncManager != null && PlayerIconManager.Instance != null)
        {
            UpdatePlayerIcons();
        }
    }
    
    private void OnDestroy()
    {
        if (gameSyncManager != null)
        {
            gameSyncManager.OnPlayerStatesSync -= OnPlayerStatesSync;
        }
        
        if (PlayerIconManager.Instance != null)
        {
            PlayerIconManager.Instance.OnIconSet -= OnIconSet;
        }
        
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        
        CancelInvoke();
    }

    private void OnPlayerStatesSync(ulong[] aliveClientIds)
    {
        UpdatePlayerIcons();
    }

    private void UpdatePlayerIcons()
    {
        if (PlayerIconManager.Instance == null)
        {
            if(debugMode) Debug.LogWarning("PlayerIconDisplay: PlayerIconManager is null");
            return;
        }

        // Get all connected clients from NetworkManager
        HashSet<ulong> connectedClientIds = new HashSet<ulong>();
        
        if (NetworkManager.Singleton != null)
        {
            // Get all connected clients
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                connectedClientIds.Add(clientId);
            }
        }
        
        // get players from GameSyncManager 
        if (gameSyncManager != null)
        {
            var allPlayers = gameSyncManager.GetAlivePlayers().ToList();
            foreach (var player in allPlayers)
            {
                connectedClientIds.Add(player.ClientId);
            }
        }
        
        if(debugMode) Debug.Log($"PlayerIconDisplay: Found {connectedClientIds.Count} connected clients");
        
        if (connectedClientIds.Count == 0)
        {
            // No clients connected yet
            return;
        }
        
        var currentClientIds = connectedClientIds;

        // Deactivate icons for disconnected players
        var disconnectedIds = playerToSlotMap.Keys.Where(id => !currentClientIds.Contains(id)).ToList();
        foreach (var id in disconnectedIds)
        {
            if (playerToSlotMap.TryGetValue(id, out int slotIndex))
            {
                if (slotIndex >= 0 && slotIndex < playerIconImages.Length && playerIconImages[slotIndex] != null)
                {
                    playerIconImages[slotIndex].gameObject.SetActive(false);
                }
                playerToSlotMap.Remove(id);
            }
        }

        // Assign slots and update icons for all connected clients
        foreach (ulong clientId in currentClientIds)
        {
            // Check if this client has an icon
            Sprite icon = PlayerIconManager.Instance.GetPlayerIcon(clientId);
            
            if (icon != null)
            {
                if (!playerToSlotMap.ContainsKey(clientId))
                {
                    // Assign a new slot
                    if(debugMode) Debug.Log($"PlayerIconDisplay: Assigning slot for client {clientId} (has icon)");
                    AssignSlotToPlayer(clientId);
                }
                else
                {
                    // Update existing slot
                    UpdatePlayerIconUI(clientId);
                }
            }
        }
    }
    
    private void AssignSlotToPlayer(ulong clientId)
    {
        // Find next available slot
        int slotIndex = -1;
        for (int i = 0; i < playerIconImages.Length; i++)
        {
            if (playerIconImages[i] != null && !playerIconImages[i].gameObject.activeSelf)
            {
                slotIndex = i;
                break;
            }
        }
        
        // If no available slot, use nextAvailableSlot
        if (slotIndex == -1)
        {
            slotIndex = nextAvailableSlot % playerIconImages.Length;
            nextAvailableSlot++;
        }
        
        if (slotIndex >= 0 && slotIndex < playerIconImages.Length && playerIconImages[slotIndex] != null)
        {
            playerToSlotMap[clientId] = slotIndex;
            playerIconImages[slotIndex].gameObject.SetActive(true);
            if(debugMode) Debug.Log($"PlayerIconDisplay: Assigned client {clientId} to slot {slotIndex}");
            UpdatePlayerIconUI(clientId);
        }
    }

    private void UpdatePlayerIconUI(ulong clientId)
    {
        if (!playerToSlotMap.TryGetValue(clientId, out int slotIndex))
        {
            if(debugMode) Debug.LogWarning($"PlayerIconDisplay: No slot mapped for client {clientId}");
            return;
        }
        
        if (slotIndex < 0 || slotIndex >= playerIconImages.Length || playerIconImages[slotIndex] == null)
        {
            if(debugMode) Debug.LogWarning($"PlayerIconDisplay: Invalid slot index {slotIndex} for client {clientId}");
            return;
        }

        // Get icon from PlayerIconManager
        Sprite icon = PlayerIconManager.Instance.GetPlayerIcon(clientId);
        
        if (icon != null)
        {
            playerIconImages[slotIndex].sprite = icon;
            if(debugMode) Debug.Log($"PlayerIconDisplay: Updated icon for client {clientId} in slot {slotIndex}");
        }
    }
}