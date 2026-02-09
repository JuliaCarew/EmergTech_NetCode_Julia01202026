using System.Linq;
using CrocoType.Interfaces;
using CrocoType.Domain;
using CrocoType.Networking;

namespace CrocoType.States
{
    public class ToothPickState : GameState
    {
        // config
        private const float PickTimeLimit = 10.0f; // seconds before a random tooth is auto-picked

        // next state
        private EliminationState _eliminationState;

        // injected
        private readonly ToothManager        _toothManager;

        // runtime
        private float _timeElapsed;
        private int   _toothCount;

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
            // Simple tooth count: starts at 5, increases by 1 each round
            _toothCount  = 5 + SyncManager.CurrentRound.Value;

            // server secretly picks the lethal tooth — not broadcast yet
            _toothManager.GenerateLethalTooth(_toothCount);

            // reset every loser's tooth selection
            foreach (var p in GetLosers())
                p.ResetSelection();

            // tell clients: here's how many teeth, who must pick, phase = ToothPick
            SyncManager.BroadcastToothPhaseStart(_toothCount);
            SyncManager.BroadcastGamePhase(GamePhase.ToothPick);
        }

        public override void Update()
        {
            _timeElapsed += UnityEngine.Time.deltaTime;

            var losers = GetLosers().ToList();

            // --- auto-pick for any player who hasn't chosen in time ---
            if (_timeElapsed >= PickTimeLimit)
            {
                foreach (var p in losers.Where(p => !p.HasSelected))
                    HandleToothSelection(p.ClientId, _toothManager.GetRandomSafeIndex());
            }

            // --- wait until every loser has picked ---
            if (!losers.All(p => p.HasSelected))
                return;

            // everyone has chosen — move to reveal
            TransitionTo(_eliminationState);
        }

        // ── called by SyncManager when a client's ServerRpc arrives ─────
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

            player.SelectTooth(toothIndex);

            // sync the choice to all clients so the crocodile UI updates live
            SyncManager.BroadcastToothSelection(clientId, toothIndex);
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
