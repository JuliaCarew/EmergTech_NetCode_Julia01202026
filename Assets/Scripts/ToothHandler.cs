using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using CrocoType.Networking;
using System.Collections.Generic;
using System.Linq;

public class ToothHandler : MonoBehaviour
{
    [SerializeField] private Button[] teeth;
    [SerializeField] private GameObject toothSelectionPanel; 
    
    private GameSyncManager _syncManager;
    private ulong _winnerClientId = ulong.MaxValue;
    private bool _isWinner = false;
    private int _currentToothCount = 0;
    private bool _hasSelected = false;
    private HashSet<int> _previouslySelectedTeeth = new HashSet<int>(); // Track teeth selected in previous rounds

    void Start()
    {
        // Find GameSyncManager
        _syncManager = FindObjectOfType<GameSyncManager>();
        
        if (_syncManager == null)
        {
            Debug.LogError("ToothHandler: GameSyncManager not found!");
            return;
        }

        Debug.Log($"ToothHandler: Initialized on client {NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue}, GameSyncManager found: {_syncManager != null}");

        // Subscribe to events
        _syncManager.OnWinnerAnnounced += OnWinnerAnnounced;
        _syncManager.OnToothPhaseStart += OnToothPhaseStart;
        _syncManager.OnPhaseChanged += OnPhaseChanged;

        // Set up tooth button listeners
        if (teeth != null && teeth.Length > 0)
        {
            for (int i = 0; i < teeth.Length; i++)
            {
                int toothIndex = i; // Capture index for closure
                if (teeth[i] != null)
                {
                    teeth[i].onClick.RemoveAllListeners();
                    teeth[i].onClick.AddListener(() => OnToothButtonClicked(toothIndex));
                }
            }
        }

        // Initially hide tooth buttons
        SetToothButtonsVisible(false);
    }

    void OnDestroy()
    {
        if (_syncManager != null)
        {
            _syncManager.OnWinnerAnnounced -= OnWinnerAnnounced;
            _syncManager.OnToothPhaseStart -= OnToothPhaseStart;
            _syncManager.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    private void OnWinnerAnnounced(ulong winnerClientId)
    {
        _winnerClientId = winnerClientId;
        
        // Check if local player is the winner
        if (NetworkManager.Singleton != null)
        {
            if (winnerClientId == ulong.MaxValue)
            {
                _isWinner = false; // No winner, everyone is a "loser" and picks teeth
                Debug.Log($"ToothHandler: No winner this round (everyone picks teeth)");
            }
            else
            {
                _isWinner = NetworkManager.Singleton.LocalClientId == winnerClientId;
                Debug.Log($"ToothHandler: Winner is client {winnerClientId}, local player is winner: {_isWinner}");
            }
        }
    }

    private void OnToothPhaseStart(int toothCount, int[] previouslySelectedTeeth)
    {
        _currentToothCount = toothCount;
        _hasSelected = false;
        
        // Update the set of previously selected teeth
        _previouslySelectedTeeth.Clear();
        if (previouslySelectedTeeth != null)
        {
            foreach (int toothIndex in previouslySelectedTeeth)
            {
                _previouslySelectedTeeth.Add(toothIndex);
            }
        }
        
        Debug.Log($"ToothHandler: Tooth phase started with {toothCount} teeth. Previously selected: {_previouslySelectedTeeth.Count}. Available buttons: {teeth?.Length ?? 0}. Is winner: {_isWinner}");

        // Only show tooth buttons for losing players
        if (!_isWinner)
        {
            SetToothButtonsVisible(true);
            // Enable only the valid tooth buttons (0 to toothCount-1) that haven't been selected before
            for (int i = 0; i < (teeth?.Length ?? 0); i++)
            {
                if (teeth[i] != null)
                {
                    // Button is interactable if
                    bool isWithinRange = (i < toothCount);
                    bool wasNotSelectedBefore = !_previouslySelectedTeeth.Contains(i);
                    bool isInteractable = isWithinRange && wasNotSelectedBefore;
                    
                    teeth[i].interactable = isInteractable;
                }
            }
            int enabledCount = Enumerable.Range(0, Mathf.Min(toothCount, teeth?.Length ?? 0))
                .Count(i => !_previouslySelectedTeeth.Contains(i));
            Debug.Log($"ToothHandler: Enabled {enabledCount} buttons (indices 0-{toothCount - 1}, excluding {_previouslySelectedTeeth.Count} previously selected) out of {teeth?.Length ?? 0} total buttons");
        }
        else
        {
            SetToothButtonsVisible(false);
            Debug.Log("ToothHandler: Player is winner, hiding tooth selection buttons");
        }
    }

    private void OnPhaseChanged(GamePhase phase)
    {
        // Hide tooth buttons when not in ToothPick phase
        if (phase != GamePhase.ToothPick)
        {
            SetToothButtonsVisible(false);
            _hasSelected = false;
        }
    }

    private void OnToothButtonClicked(int toothIndex)
    {
        Debug.Log($"ToothHandler: OnToothButtonClicked called with toothIndex {toothIndex} (LocalClientId: {NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue})");
        
        if (_isWinner && _winnerClientId != ulong.MaxValue)
        {
            Debug.LogWarning($"ToothHandler: Winner (client {_winnerClientId}) cannot select a tooth!");
            return;
        }

        if (_hasSelected)
        {
            Debug.LogWarning("ToothHandler: Player has already selected a tooth!");
            return;
        }

        // Client-side validation: check if the selected tooth index is valid
        if (toothIndex < 0 || toothIndex >= _currentToothCount)
        {
            Debug.LogWarning($"ToothHandler: Invalid tooth index {toothIndex} (valid range: 0-{_currentToothCount - 1}). This round only has {_currentToothCount} teeth available.");
            return;
        }
        
        // Check if this tooth was already selected in a previous round
        if (_previouslySelectedTeeth.Contains(toothIndex))
        {
            Debug.LogWarning($"ToothHandler: Tooth {toothIndex} was already selected in a previous round and cannot be selected again.");
            return;
        }

        if (_syncManager == null)
        {
            Debug.LogError("ToothHandler: GameSyncManager is null!");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            Debug.LogError("ToothHandler: NetworkManager is not available or not a client!");
            return;
        }

        Debug.Log($"ToothHandler: Sending tooth selection {toothIndex} to server via RPC");
        
        // Send selection to server
        _syncManager.SelectToothServerRpc(toothIndex);
        _hasSelected = true;
        
        Debug.Log($"ToothHandler: Player selected tooth {toothIndex}, buttons disabled");

        // Disable all buttons after selection
        SetToothButtonsInteractable(false);
    }

    private void SetToothButtonsVisible(bool visible)
    {
        if (toothSelectionPanel != null)
        {
            toothSelectionPanel.SetActive(visible);
        }
        else if (teeth != null)
        {
            foreach (var tooth in teeth)
            {
                if (tooth != null)
                {
                    tooth.gameObject.SetActive(visible);
                }
            }
        }
    }

    private void SetToothButtonsInteractable(bool interactable)
    {
        if (teeth != null)
        {
            foreach (var tooth in teeth)
            {
                if (tooth != null)
                {
                    tooth.interactable = interactable;
                }
            }
        }
    }
}
