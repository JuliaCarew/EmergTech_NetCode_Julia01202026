using CrocoType.Networking;
using CrocoType.Domain;

namespace CrocoType.States
{
    public class WaitingState : GameState
    {
        // config
        private const int   MinPlayersRequired = 2;
        private const float CountdownDuration  = 3.0f; 

        // runtime
        private float _countdownRemaining;
        private bool  _countdownStarted;

        // next state
        private TypingState _typingState;

        public WaitingState(GameStateMachine stateMachine,
                            GameSyncManager  syncManager)
            : base(stateMachine, syncManager) { }

        // Called once by GameBootstrap after all states are constructed
        public void SetNextState(TypingState typingState) => _typingState = typingState;

        // lifecycle
        public override void Enter()
        {
            _countdownStarted    = false;
            _countdownRemaining  = CountdownDuration;

            // tell all clients we are in the waiting phase
            SyncManager.BroadcastGamePhase(GamePhase.Waiting);
        }

        public override void Update()
        {
            if (SyncManager.ConnectedPlayerCount < MinPlayersRequired)
                return;

            if (!_countdownStarted)
            {
                _countdownStarted = true;
                SyncManager.BroadcastCountdown(_countdownRemaining);
            }

            // tick down to next phase
            _countdownRemaining -= UnityEngine.Time.deltaTime;
            SyncManager.BroadcastCountdown(_countdownRemaining); 

            if (_countdownRemaining <= 0f)
                TransitionTo(_typingState);
        }
    }
}
