using System.Collections.Generic;
using UnityEngine;

public class PlayerIconManager : MonoBehaviour
{
    public static PlayerIconManager Instance { get; private set; }

    private Dictionary<ulong, Sprite> playerIcons = new Dictionary<ulong, Sprite>();
    private byte[] localPlayerIconBytes = null;
    
    // Event for when an icon is set
    public System.Action<ulong> OnIconSet;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetLocalPlayerIcon(byte[] iconBytes)
    {
        localPlayerIconBytes = iconBytes;
    }

    public byte[] GetLocalPlayerIconBytes()
    {
        return localPlayerIconBytes;
    }

    public void SetPlayerIcon(ulong clientId, byte[] iconBytes)
    {
        if (iconBytes == null || iconBytes.Length == 0)
        {
            Debug.LogWarning($"PlayerIconManager: Attempted to set icon for client {clientId} but iconBytes is null or empty");
            return;
        }

        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(iconBytes))
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            playerIcons[clientId] = sprite;
            
            Debug.Log($"PlayerIconManager: Set icon for client {clientId}, total icons: {playerIcons.Count}");
            
            // Notify that an icon was set
            OnIconSet?.Invoke(clientId);
        }
    }
    
    // Set the local player's icon using their client ID
    public void SetLocalPlayerIconWithId(ulong clientId)
    {
        if (localPlayerIconBytes != null)
        {
            SetPlayerIcon(clientId, localPlayerIconBytes);
        }
    }

    public Sprite GetPlayerIcon(ulong clientId)
    {
        if (playerIcons.TryGetValue(clientId, out Sprite icon))
        {
            return icon;
        }
        return null;
    }

    public void RemovePlayerIcon(ulong clientId)
    {
        playerIcons.Remove(clientId);
    }

    public Dictionary<ulong, Sprite> GetAllPlayerIcons()
    {
        return new Dictionary<ulong, Sprite>(playerIcons);
    }
}