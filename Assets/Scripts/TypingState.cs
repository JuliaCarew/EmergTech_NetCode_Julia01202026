using System.Collections.Generic;
using System.Linq;
using CrocoType.Interfaces;
using CrocoType.Domain;
using CrocoType.Networking;

namespace CrocoType.States
{
    public class TypingState : GameState
    {
        // config
        private const float TypingTimeLimit = 30.0f; // max seconds before auto-fail

        // next state
        private ToothPickState _toothPickState;

        public TypingState(GameStateMachine  stateMachine,
                           GameSyncManager   syncManager,
                           ISentenceProvider sentenceProvider)
            : base(stateMachine, syncManager)
        {
            _sentenceProvider = sentenceProvider;
        }

        public void SetNextState(ToothPickState toothPickState) => _toothPickState = toothPickState;

        private readonly ISentenceProvider _sentenceProvider;

        // runtime
        private string _currentSentence;
        private float _timeElapsed;
        private ulong _winnerClientId;

        // public accessors (read by other states / UI)
        public string CurrentSentence => _currentSentence;
        public ulong  WinnerClientId  => _winnerClientId;

        // lifecycle
        public override void Enter()
        {
            _timeElapsed = 0f;
            _winnerClientId = ulong.MaxValue; // sentinel = no winner yet

            // 1. fetch random sentence (server-side only)
            // Check if SentenceGenerator has a sentence set (from Start button), otherwise generate new one
            var sentenceGenerator = UnityEngine.Object.FindObjectOfType<SentenceGenerator>();
            if (sentenceGenerator != null && !string.IsNullOrEmpty(sentenceGenerator.Sentence.Value.Value))
            {
                _currentSentence = sentenceGenerator.Sentence.Value.Value;
            }
            else
            {
                _currentSentence = _sentenceProvider.GetSentence();
                // If we generated a new sentence, also update SentenceGenerator so UI stays in sync
                if (sentenceGenerator != null)
                {
                    sentenceGenerator.Sentence.Value = new NetworkString(_currentSentence);
                }
            }

            // 2. reset every alive player's typing state
            foreach (var p in SyncManager.GetAlivePlayers())
                p.ResetInput();

            // 3. push sentence + phase to all clients in one RPC
            SyncManager.BroadcastRoundStart(_currentSentence, SyncManager.CurrentRound.Value);
            SyncManager.BroadcastGamePhase(GamePhase.Typing);
        }

        public override void Update()
        {
            _timeElapsed += UnityEngine.Time.deltaTime;

            // --- check if every alive player has finished OR time ran out ---
            var alivePlayers = SyncManager.GetAlivePlayers().ToList();
            bool allDone     = alivePlayers.All(p => p.HasFinished);
            bool timeExpired = _timeElapsed >= TypingTimeLimit;

            if (!allDone && !timeExpired)
                return; // still racing

            // --- determine the winner (lowest completion time among finishers) ---
            var finishers = alivePlayers.Where(p => p.HasFinished).ToList();

            if (finishers.Count > 0)
            {
                _winnerClientId = finishers
                    .OrderBy(p => p.CompletionTime)
                    .First()
                    .ClientId;
            }
            // else: nobody finished in time — no winner this round, everyone picks teeth

            SyncManager.BroadcastWinner(_winnerClientId);
            TransitionTo(_toothPickState);
        }

        public void HandlePlayerInput(ulong clientId, string typedText, float timestamp)
        {
            var player = SyncManager.GetPlayerByClientId(clientId);
            if (player == null || player.HasFinished)
                return;

            // feed characters one by one (keeps TypingEvaluator stateless & reusable)
            foreach (char c in typedText)
                player.SubmitCharacter(c);

            // check full completion
            if (TypingEvaluator.IsComplete(typedText, _currentSentence))
                player.MarkFinished(timestamp);
        }
    }
}
