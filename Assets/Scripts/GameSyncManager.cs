using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using CrocoType.Domain;
using CrocoType.States;

namespace CrocoType.Networking
{
    public enum GamePhase : byte
    {
        Waiting,
        Typing,
        ToothPick,
        Elimination,
        GameOver
    }

    public class GameSyncManager : NetworkBehaviour
    {
        public NetworkVariable<int>       CurrentRound    = new(0);
        public NetworkVariable<GamePhase> CurrentPhase    = new(GamePhase.Waiting);

        private readonly Dictionary<ulong, Player> _players = new();
        
        // Store player icons on server to send to newly connected clients
        private readonly Dictionary<ulong, byte[]> _playerIcons = new Dictionary<ulong, byte[]>();
        
        // Track teeth that have been selected in previous rounds (to disable them in future rounds)
        private readonly HashSet<int> _previouslySelectedTeeth = new HashSet<int>();

        private TypingState _typingState; 

        public void SetTypingState(TypingState typingState) => _typingState = typingState;

        public void RegisterPlayer(Player player)
        {
            _players[player.ClientId] = player;
        }

        public void UnregisterPlayer(ulong clientId)
        {
            _players.Remove(clientId);
        }

        public Player GetPlayerByClientId(ulong id) =>
            _players.TryGetValue(id, out var p) ? p : null;

        public IEnumerable<Player> GetAlivePlayers() =>
            _players.Values.Where(p => p.IsAlive);
        
        public bool IsToothPreviouslySelected(int toothIndex) =>
            _previouslySelectedTeeth.Contains(toothIndex);

        public int ConnectedPlayerCount => _players.Count;

        public void IncrementRound() => CurrentRound.Value++;

        public void BroadcastGamePhase(GamePhase phase)
        {
            CurrentPhase.Value = phase;
            // Only send RPC if NetworkManager is started
            if (IsServer && IsSpawned)
            {
                RpcNotifyPhaseChangeClientRpc(phase);
            }
        }

        [ClientRpc]
        private void RpcNotifyPhaseChangeClientRpc(GamePhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
        }

        public void BroadcastCountdown(float secondsLeft)
        {
            // Only send RPC if NetworkManager is started
            if (IsServer && IsSpawned)
            {
                RpcCountdownClientRpc(secondsLeft);
            }
        }

        [ClientRpc]
        private void RpcCountdownClientRpc(float secondsLeft)
        {
            OnCountdownUpdate?.Invoke(secondsLeft);
        }

        public void BroadcastRoundStart(string sentence, int roundNumber)
        {
            // Only send RPC if NetworkManager is started and we're on the server
            if (IsServer && IsSpawned)
            {
                RpcRoundStartClientRpc(sentence, roundNumber);
            }
        }

        [ClientRpc]
        private void RpcRoundStartClientRpc(string sentence, int roundNumber)
        {
            OnRoundStart?.Invoke(sentence, roundNumber);
        }

        public void BroadcastWinner(ulong winnerClientId)
        {
            if (IsServer && IsSpawned)
            {
                RpcWinnerClientRpc(winnerClientId);
            }
        }

        [ClientRpc]
        private void RpcWinnerClientRpc(ulong winnerClientId)
        {
            OnWinnerAnnounced?.Invoke(winnerClientId);
        }

        public void BroadcastToothPhaseStart(int toothCount)
        {
            // Convert HashSet to array for RPC
            int[] previouslySelectedArray = _previouslySelectedTeeth.ToArray();
            RpcToothPhaseStartClientRpc(toothCount, previouslySelectedArray);
        }

        [ClientRpc]
        private void RpcToothPhaseStartClientRpc(int toothCount, int[] previouslySelectedTeeth)
        {
            OnToothPhaseStart?.Invoke(toothCount, previouslySelectedTeeth);
        }

        public void BroadcastToothSelection(ulong clientId, int toothIndex)
        {
            // Track this tooth as selected (on server only)
            if (IsServer)
            {
                _previouslySelectedTeeth.Add(toothIndex);
                Debug.Log($"GameSyncManager: Tooth {toothIndex} selected by client {clientId}. Total previously selected: {_previouslySelectedTeeth.Count}");
            }
            
            RpcToothSelectedClientRpc(clientId, toothIndex);
        }

        [ClientRpc]
        private void RpcToothSelectedClientRpc(ulong clientId, int toothIndex)
        {
            OnToothSelected?.Invoke(clientId, toothIndex);
        }

        public void BroadcastLethalTooth(int lethalIndex)
        {
            RpcRevealLethalToothClientRpc(lethalIndex);
        }

        [ClientRpc]
        private void RpcRevealLethalToothClientRpc(int lethalIndex)
        {
            OnLethalToothRevealed?.Invoke(lethalIndex);
        }

        public void BroadcastPlayerEliminated(ulong eliminatedClientId, int lethalToothIndex)
        {
            if (IsServer && IsSpawned)
            {
                RpcPlayerEliminatedClientRpc(eliminatedClientId, lethalToothIndex);
            }
        }

        [ClientRpc]
        private void RpcPlayerEliminatedClientRpc(ulong eliminatedClientId, int lethalToothIndex)
        {
            OnPlayerEliminated?.Invoke(eliminatedClientId, lethalToothIndex);
        }

        public void BroadcastGameOver(ulong winnerClientId)
        {
            if (IsServer && IsSpawned)
            {
                CurrentPhase.Value = GamePhase.GameOver;
                RpcGameOverClientRpc(winnerClientId);
            }
        }

        [ClientRpc]
        private void RpcGameOverClientRpc(ulong winnerClientId)
        {
            OnGameOver?.Invoke(winnerClientId);
        }

        public void SyncPlayerStates()
        {
            var aliveIds = GetAlivePlayers().Select(p => p.ClientId).ToArray();
            RpcSyncAlivePlayersClientRpc(aliveIds);
        }

        [ClientRpc]
        private void RpcSyncAlivePlayersClientRpc(ulong[] aliveClientIds)
        {
            OnPlayerStatesSync?.Invoke(aliveClientIds);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SubmitTypingInputServerRpc(string typedSoFar, float timestamp, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"GameSyncManager: Received typing input from client {clientId}, text length: {typedSoFar?.Length ?? 0}, timestamp: {timestamp}");
            _typingState?.HandlePlayerInput(clientId, typedSoFar, timestamp);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectToothServerRpc(int toothIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"GameSyncManager: Received tooth selection from client {clientId}, toothIndex: {toothIndex}");
            OnToothSelectionReceived?.Invoke(clientId, toothIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerIconServerRpc(byte[] iconBytes, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"GameSyncManager: Received icon from client {clientId}, broadcasting to all clients (iconBytes length: {iconBytes?.Length ?? 0})");
            
            if (iconBytes == null || iconBytes.Length == 0)
            {
                Debug.LogError($"GameSyncManager: Received null or empty iconBytes from client {clientId}");
                return;
            }
            
            // Store the icon on the server
            _playerIcons[clientId] = iconBytes;
            
            // Broadcast the icon to all clients
            RpcSetPlayerIconClientRpc(clientId, iconBytes);
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (IsServer)
            {
                // Register the host/server as a player
                RegisterPlayerForClient(NetworkManager.Singleton.LocalClientId);
                
                // Subscribe to client connection events to send existing icons
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedServer;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedServer;
            }
            
            base.OnNetworkDespawn();
        }
        
        private void OnClientConnectedServer(ulong clientId)
        {
            if (!IsServer) return;
            
            RegisterPlayerForClient(clientId);
            
            StartCoroutine(SendIconsToNewClient(clientId));
        }
        
        private void RegisterPlayerForClient(ulong clientId)
        {
            // Check if player is already registered
            if (_players.ContainsKey(clientId))
            {
                Debug.Log($"GameSyncManager: Player {clientId} is already registered");
                return;
            }
            
            // Create and register a new Player domain object
            string playerName = $"Player {clientId}";
            Player player = new Player(clientId, playerName);
            RegisterPlayer(player);
            Debug.Log($"GameSyncManager: Registered player {clientId} ({playerName})");
        }
        
        private System.Collections.IEnumerator SendIconsToNewClient(ulong clientId)
        {
            // Wait a frame to ensure client is fully connected
            yield return null;
            
            // Send all existing icons to the newly connected client
            Debug.Log($"GameSyncManager: Client {clientId} connected, sending {_playerIcons.Count} existing icons");
            foreach (var kvp in _playerIcons)
            {
                // Use a targeted ClientRpc to send to this specific client
                RpcSetPlayerIconClientRpc(kvp.Key, kvp.Value, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { clientId }
                    }
                });
                yield return null; // Small delay between sends
            }
        }

        [ClientRpc]
        private void RpcSetPlayerIconClientRpc(ulong clientId, byte[] iconBytes, ClientRpcParams rpcParams = default)
        {
            if (NetworkManager.Singleton != null)
            {
                Debug.Log($"GameSyncManager: RPC received for client {clientId} icon on client {NetworkManager.Singleton.LocalClientId} (iconBytes length: {iconBytes?.Length ?? 0})");
            }
            
            if (iconBytes == null || iconBytes.Length == 0)
            {
                Debug.LogError($"GameSyncManager: Received null or empty iconBytes in RPC for client {clientId}");
                return;
            }
            
            // Update the icon in PlayerIconManager
            if (PlayerIconManager.Instance != null)
            {
                PlayerIconManager.Instance.SetPlayerIcon(clientId, iconBytes);
            }
        }

        // ── events (UI and states subscribe to these) ──────────────────────
        public event System.Action<GamePhase>              OnPhaseChanged;
        public event System.Action<float>                  OnCountdownUpdate;
        public event System.Action<string, int>            OnRoundStart; // sentence, round
        public event System.Action<ulong>                  OnWinnerAnnounced;
        public event System.Action<int, int[]>             OnToothPhaseStart; // toothCount, previouslySelectedTeeth
        public event System.Action<ulong, int>             OnToothSelected; // clientId, index
        public event System.Action<int>                    OnLethalToothRevealed;
        public event System.Action<ulong>                  OnGameOver; // winner clientId
        public event System.Action<ulong, int>              OnPlayerEliminated; // eliminatedClientId, lethalToothIndex
        public event System.Action<ulong[]>                OnPlayerStatesSync;
        public event System.Action<ulong, int>             OnToothSelectionReceived; // server-side routing
    }
}
