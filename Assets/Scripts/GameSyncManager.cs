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
        [SerializeField] private SentenceGenerator _sentenceGenerator;

        public NetworkVariable<int> CurrentRound = new(0);
        public NetworkVariable<GamePhase> CurrentPhase = new(GamePhase.Waiting);

        private readonly Dictionary<ulong, Player> _players = new();

        public void RegisterPlayer(Player player)
        {
            _players[player.ClientId] = player;
        }

        public void UnregisterPlayer(ulong clientId)
        {
            _players.Remove(clientId);
        }

        public Player                          
            GetPlayerByClientId(ulong id) =>
            _players.TryGetValue(id, out var p) ? p : null;

        public IEnumerable<Player>             
            GetAlivePlayers() =>
            _players.Values.Where(p => p.IsAlive);

        public int ConnectedPlayerCount => _players.Count;

        public void IncrementRound() => CurrentRound.Value++;

        public void BroadcastGamePhase(GamePhase phase)
        {
            CurrentPhase.Value = phase;
            RpcNotifyPhaseChangeClientRpc(phase);
        }

        [ClientRpc]
        private void RpcNotifyPhaseChangeClientRpc(GamePhase phase)
        {
            OnPhaseChanged?.Invoke(phase);
        }

        public void BroadcastCountdown(float secondsLeft)
        {
            RpcCountdownClientRpc(secondsLeft);
        }

        [ClientRpc]
        private void RpcCountdownClientRpc(float secondsLeft)
        {
            OnCountdownUpdate?.Invoke(secondsLeft);
        }

        public void BroadcastRoundStart(int roundNumber)
        {
            Debug.Log($"BroadcastRoundStart called with roundNumber: {roundNumber}, IsServer: {IsServer}");
            
            // Only server can generate and broadcast sentences
            if (!IsServer)
            {
                Debug.LogWarning("Only server can broadcast round start!");
                return;
            }

            // Generate a new sentence for this round
            if (_sentenceGenerator != null)
            {
                Debug.Log("Calling UpdateSentence on SentenceGenerator...");
                _sentenceGenerator.UpdateSentence();
                // Get the generated sentence from the NetworkVariable
                string sentence = _sentenceGenerator.Sentence.Value.Value;
                Debug.Log($"Broadcasting sentence to clients: '{sentence}'");
                RpcRoundStartClientRpc(sentence, roundNumber);
            }
            else
            {
                Debug.LogError("SentenceGenerator is not assigned in GameSyncManager!");
            }
        }

        // Test method to start a round (for testing purposes)
        [ContextMenu("Test: Start Round 1")]
        public void TestStartRound()
        {
            if (IsServer)
            {
                IncrementRound();
                BroadcastRoundStart(CurrentRound.Value);
            }
            else
            {
                Debug.LogWarning("TestStartRound can only be called on server!");
            }
        }

        [ClientRpc]
        private void RpcRoundStartClientRpc(string sentence, int roundNumber)
        {
            OnRoundStart?.Invoke(sentence, roundNumber);
        }

        public void BroadcastWinner(ulong winnerClientId)
        {
            RpcWinnerClientRpc(winnerClientId);
        }

        [ClientRpc]
        private void RpcWinnerClientRpc(ulong winnerClientId)
        {
            OnWinnerAnnounced?.Invoke(winnerClientId);
        }

        public void BroadcastToothPhaseStart(int toothCount)
        {
            RpcToothPhaseStartClientRpc(toothCount);
        }

        [ClientRpc]
        private void RpcToothPhaseStartClientRpc(int toothCount)
        {
            OnToothPhaseStart?.Invoke(toothCount);
        }

        public void BroadcastToothSelection(ulong clientId, int toothIndex)
        {
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

        public void BroadcastGameOver(ulong winnerClientId)
        {
            CurrentPhase.Value = GamePhase.GameOver;
            RpcGameOverClientRpc(winnerClientId);
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

        [ServerRpc]
        public void SubmitTypingInputServerRpc(string typedSoFar, float timestamp, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
        }

        [ServerRpc]
        public void SelectToothServerRpc(int toothIndex, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            OnToothSelectionReceived?.Invoke(clientId, toothIndex);
        }

        // events
        public event System.Action<GamePhase> OnPhaseChanged;
        public event System.Action<float> OnCountdownUpdate;
        public event System.Action<string, int> OnRoundStart; // sentence, round
        public event System.Action<ulong> OnWinnerAnnounced;
        public event System.Action<int> OnToothPhaseStart; // toothCount
        public event System.Action<ulong, int> OnToothSelected; // clientId, index
        public event System.Action<int> OnLethalToothRevealed;
        public event System.Action<ulong> OnGameOver; // winner clientId
        public event System.Action<ulong[]> OnPlayerStatesSync;
        public event System.Action<ulong, int> OnToothSelectionReceived; // server-side routing
    }
}
