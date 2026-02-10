using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
                           GameSyncManager   syncManager)
            : base(stateMachine, syncManager)
        {
        }

        public void SetNextState(ToothPickState toothPickState) => _toothPickState = toothPickState;

        // runtime
        private string _currentSentence;
        private float _timeElapsed;
        private ulong _winnerClientId;
        private bool _roundEnded = false; // Track if round has already ended

        // public accessors (read by other states / UI)
        public string CurrentSentence => _currentSentence;
        public ulong  WinnerClientId  => _winnerClientId;

        // lifecycle
        public override void Enter()
        {
            _timeElapsed = 0f;
            _winnerClientId = ulong.MaxValue; 
            _roundEnded = false;

            // Get sentence from SentenceGenerator (generate one if it doesn't have one)
            var sentenceGenerator = Object.FindObjectOfType<SentenceGenerator>();
            if (sentenceGenerator != null)
            {
                // If SentenceGenerator doesn't have a sentence, generate one
                if (string.IsNullOrEmpty(sentenceGenerator.Sentence.Value.Value))
                {
                    sentenceGenerator.UpdateSentence();
                }
                _currentSentence = sentenceGenerator.Sentence.Value.Value;
            }
            else
            {
                Debug.LogError("TypingState: SentenceGenerator not found! Cannot get sentence.");
                _currentSentence = "";
            }

            // reset every alive player's typing state
            foreach (var p in SyncManager.GetAlivePlayers())
                p.ResetInput();

            // push sentence + phase to all clients in one RPC
            SyncManager.BroadcastRoundStart(_currentSentence, SyncManager.CurrentRound.Value);
            SyncManager.BroadcastGamePhase(GamePhase.Typing);
        }

        public override void Update()
        {
            if (_roundEnded)
                return;

            _timeElapsed += UnityEngine.Time.deltaTime;

            // check if time ran out
            bool timeExpired = _timeElapsed >= TypingTimeLimit;

            if (timeExpired)
            {
                // Time expired - determine winner from finishers, or no winner if none finished
                var alivePlayers = SyncManager.GetAlivePlayers().ToList();
                var finishers = alivePlayers.Where(p => p.HasFinished).ToList();

                if (finishers.Count > 0)
                {
                    _winnerClientId = finishers
                        .OrderBy(p => p.CompletionTime)
                        .First()
                        .ClientId;
                }

                EndRound();
            }
        }
        
        private void EndRound()
        {
            if (_roundEnded)
                return;
            
            _roundEnded = true;
            SyncManager.BroadcastWinner(_winnerClientId);
            TransitionTo(_toothPickState);
        }

        public void HandlePlayerInput(ulong clientId, string typedText, float timestamp)
        {
            // If round already ended, ignore input
            if (_roundEnded)
            {
                Debug.Log($"TypingState: Round already ended, ignoring input from client {clientId}");
                return;
            }

            var player = SyncManager.GetPlayerByClientId(clientId);
            if (player == null)
            {
                Debug.LogWarning($"TypingState: Player with clientId {clientId} not found! Registered players: {string.Join(", ", SyncManager.GetAlivePlayers().Select(p => p.ClientId))}");
                return;
            }
            
            if (player.HasFinished)
            {
                Debug.Log($"TypingState: Player {clientId} has already finished, ignoring input");
                return;
            }

            Debug.Log($"TypingState: Processing input from client {clientId}, typedText: '{typedText}', currentSentence: '{_currentSentence}'");

            // feed characters one by one 
            foreach (char c in typedText)
                player.SubmitCharacter(c);

            // check full completion
            if (TypingEvaluator.IsComplete(typedText, _currentSentence))
            {
                Debug.Log($"TypingState: Client {clientId} completed the sentence! Marking as finished.");
                player.MarkFinished(timestamp);
                
                // Check if this is the first player to finish
                var alivePlayers = SyncManager.GetAlivePlayers().ToList();
                var finishers = alivePlayers.Where(p => p.HasFinished).ToList();
                
                Debug.Log($"TypingState: Finishers count: {finishers.Count}, Total alive players: {alivePlayers.Count}");
                
                // If this is the first finisher, immediately end the round
                if (finishers.Count == 1)
                {
                    _winnerClientId = clientId;
                    Debug.Log($"TypingState: Client {clientId} is the first finisher! Setting as winner and ending round.");
                    EndRound();
                }
            }
        }
    }
}
