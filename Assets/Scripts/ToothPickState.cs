using System.Collections.Generic;
using System.Linq;
using CrocoType.Interfaces;
using CrocoType.Domain;
using CrocoType.Networking;

namespace CrocoType.States
{
    public class ToothPickState : GameState
    {
        // config
        private const float PickTimeLimit = 30.0f; // seconds before a random tooth is auto-picked

        // next state
        private EliminationState _eliminationState;

        // injected
        private readonly ToothManager        _toothManager;

        // runtime
        private float _timeElapsed;
        private int   _toothCount;
        private bool  _gameOver = false; // Track if game has ended

        public ToothPickState(GameStateMachine  stateMachine,
                              GameSyncManager   syncManager,
                              ToothManager      toothManager)
            : base(stateMachine, syncManager)
        {
            _toothManager       = toothManager;
        }

        public void SetNextState(EliminationState eliminationState) => _eliminationState = eliminationState;

        // lifecycle
        public override void Enter()
        {
            _timeElapsed = 0f;
            _gameOver = false;
            
            _toothCount  = 10 + SyncManager.CurrentRound.Value;

            // server secretly picks the lethal tooth 
            _toothManager.GenerateLethalTooth(_toothCount);
            
            UnityEngine.Debug.Log($"ToothPickState: LETHAL TOOTH GENERATED - Tooth index {_toothManager.LethalToothIndex} is the DEATH TOOTH (out of {_toothCount} total teeth)");

            // reset every loser's tooth selection
            foreach (var p in GetLosers())
                p.ResetSelection();

            // tell clients: here's how many teeth, who must pick, phase = ToothPick
            SyncManager.BroadcastToothPhaseStart(_toothCount);
            SyncManager.BroadcastGamePhase(GamePhase.ToothPick);
        }

        public override void Update()
        {
            // Don't process if game is over
            if (_gameOver)
                return;

            _timeElapsed += UnityEngine.Time.deltaTime;

            var losers = GetLosers().ToList();

            // auto-pick for any player who hasn't chosen in time
            if (_timeElapsed >= PickTimeLimit)
            {
                foreach (var p in losers.Where(p => !p.HasSelected))
                {
                    // Find an available tooth
                    int availableTooth = GetRandomAvailableTooth();
                    if (availableTooth >= 0)
                    {
                        HandleToothSelection(p.ClientId, availableTooth);
                    }
                }
            }

            // wait until every loser has picked 
            if (!losers.All(p => p.HasSelected))
                return;

            // everyone has chosen — move to reveal (only if game hasn't ended)
            if (!_gameOver)
            {
                TransitionTo(_eliminationState);
            }
        }

        // called by SyncManager when a client's ServerRpc arrives 
        public void HandleToothSelection(ulong clientId, int toothIndex)
        {
            // only losers may pick
            if (clientId == TypingState_WinnerClientId())
                return;

            var player = SyncManager.GetPlayerByClientId(clientId);
            if (player == null || player.HasSelected)
                return;

            // clamp to valid range
            if (toothIndex < 0 || toothIndex >= _toothCount)
                return;
            
            // Check if this tooth was already selected in a previous round
            if (SyncManager.IsToothPreviouslySelected(toothIndex))
            {
                UnityEngine.Debug.LogWarning($"ToothPickState: Tooth {toothIndex} was already selected in a previous round. Rejecting selection from client {clientId}.");
                return;
            }

            player.SelectTooth(toothIndex);

            // sync the choice to all clients so the crocodile UI updates live
            SyncManager.BroadcastToothSelection(clientId, toothIndex);
            
            // Check if the selected tooth is lethal - if so, eliminate the player immediately
            if (_toothManager.IsLethal(toothIndex))
            {
                UnityEngine.Debug.Log($"ToothPickState: Player {clientId} selected the DEATH TOOTH (index {toothIndex})! Eliminating player immediately.");
                int currentRound = SyncManager.CurrentRound.Value;
                player.Eliminate(currentRound);
                
                // Broadcast that this player was eliminated (for UI to show lose screen)
                SyncManager.BroadcastPlayerEliminated(clientId, toothIndex);
                
                // Check if game should end (only one player left or all players eliminated)
                var alivePlayers = SyncManager.GetAlivePlayers().ToList();
                if (alivePlayers.Count <= 1)
                {
                    _gameOver = true;
                    ulong winnerId = alivePlayers.Count == 1 ? alivePlayers[0].ClientId : ulong.MaxValue;
                    UnityEngine.Debug.Log($"ToothPickState: Game Over! Winner: {(winnerId == ulong.MaxValue ? "None" : $"Client {winnerId}")}");
                    SyncManager.BroadcastGameOver(winnerId);
                }
            }
        }

        // helpers
        private IEnumerable<Player> GetLosers()
        {
            // alive players minus the typing winner
            ulong winnerId = TypingState_WinnerClientId();
            return SyncManager.GetAlivePlayers()
                              .Where(p => p.ClientId != winnerId);
        }

        // Pulls the winner ID from the typing phase via the state machine
        private ulong TypingState_WinnerClientId() =>
            StateMachine.GetState<TypingState>()?.WinnerClientId ?? ulong.MaxValue;
        
        // Get a random tooth that hasn't been selected before and isn't lethal
        private int GetRandomAvailableTooth()
        {
            var availableTeeth = new List<int>();
            
            for (int i = 0; i < _toothCount; i++)
            {
                // Check if tooth is available
                if (!SyncManager.IsToothPreviouslySelected(i) && !_toothManager.IsLethal(i))
                {
                    availableTeeth.Add(i);
                }
            }
            
            if (availableTeeth.Count == 0)
                return -1; // No available teeth
            
            // Return a random available tooth
            int randomIndex = UnityEngine.Random.Range(0, availableTeeth.Count);
            return availableTeeth[randomIndex];
        }
    }
}
