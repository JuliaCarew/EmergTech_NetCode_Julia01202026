using System.Linq;
using CrocoType.Domain;
using CrocoType.Networking;

namespace CrocoType.States
{
    public class EliminationState : GameState
    {
        // next state
        private WaitingState _waitingState;

        // injected
        private readonly ToothManager _toothManager;

        public EliminationState(GameStateMachine stateMachine,
                               GameSyncManager  syncManager,
                               ToothManager     toothManager)
            : base(stateMachine, syncManager)
        {
            _toothManager = toothManager;
        }

        public void SetNextState(WaitingState waitingState) => _waitingState = waitingState;

        // lifecycle
        public override void Enter()
        {
            // reveal the lethal tooth to all clients
            int lethalIndex = _toothManager.LethalToothIndex;
            SyncManager.BroadcastLethalTooth(lethalIndex);

            // eliminate players who picked the lethal tooth
            var losers = GetLosers().ToList();
            int currentRound = SyncManager.CurrentRound.Value;

            foreach (var player in losers)
            {
                if (player.HasSelected && _toothManager.IsLethal(player.SelectedToothIndex))
                {
                    player.Eliminate(currentRound);
                }
            }

            // check if game is over (only one player left)
            var alivePlayers = SyncManager.GetAlivePlayers().ToList();
            if (alivePlayers.Count <= 1)
            {
                ulong winnerId = alivePlayers.Count == 1 ? alivePlayers[0].ClientId : ulong.MaxValue;
                SyncManager.BroadcastGameOver(winnerId);
            }
            else
            {
                // move to next round
                SyncManager.IncrementRound();
                SyncManager.BroadcastGamePhase(GamePhase.Elimination);
                
                // transition back to waiting after a brief delay
                // For now, we'll transition immediately - you can add a delay if needed
                TransitionTo(_waitingState);
            }
        }

        // helpers
        private System.Collections.Generic.IEnumerable<Player> GetLosers()
        {
            // alive players minus the typing winner
            ulong winnerId = TypingState_WinnerClientId();
            return SyncManager.GetAlivePlayers()
                              .Where(p => p.ClientId != winnerId);
        }

        // Pulls the winner ID from the typing phase via the state machine
        private ulong TypingState_WinnerClientId() =>
            StateMachine.GetState<TypingState>()?.WinnerClientId ?? ulong.MaxValue;
    }
}

