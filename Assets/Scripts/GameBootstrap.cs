using UnityEngine;
using Unity.Netcode;
using CrocoType.Domain;
using CrocoType.Interfaces;
using CrocoType.Networking;
using CrocoType.Providers;
using CrocoType.States;

namespace CrocoType
{
    public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameStateMachine _stateMachine;
    [SerializeField] private GameSyncManager  _syncManager;

    private ToothManager _toothManager;
    private WaitingState _waitingState;
    private TypingState _typingState;
    private ToothPickState _toothPickState;
    private EliminationState _eliminationState;
    private bool _statesInitialized = false;
    
    private void Awake()
    {
        ISentenceProvider sentenceProvider = new SentenceProvider();

        _toothManager = new ToothManager();

        // set up states
        _waitingState = new WaitingState(_stateMachine, _syncManager);
        _typingState = new TypingState(_stateMachine, _syncManager, sentenceProvider);
        _toothPickState = new ToothPickState(_stateMachine, _syncManager, _toothManager);
        _eliminationState = new EliminationState(_stateMachine, _syncManager, _toothManager);

        // set & register states
        _waitingState.SetNextState(_typingState);
        _typingState.SetNextState(_toothPickState);
        _toothPickState.SetNextState(_eliminationState);
        _eliminationState.SetNextState(_waitingState);

        _stateMachine.RegisterState(_waitingState);
        _stateMachine.RegisterState(_typingState);
        _stateMachine.RegisterState(_toothPickState);
        _stateMachine.RegisterState(_eliminationState);

        _syncManager.SetTypingState(_typingState);

        _syncManager.OnToothSelectionReceived += _toothPickState.HandleToothSelection;
    }

    private void Start()
    {
        // Subscribe to NetworkManager events to initialize state after network starts
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
    }

    private void Update()
    {
        // Fallback: Check if we can initialize the state (in case OnServerStarted was missed or timing is off)
        if (!_statesInitialized && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            TryInitializeState();
        }
    }

    private void OnServerStarted()
    {
        TryInitializeState();
    }

    private void TryInitializeState()
    {
        // Only initialize the state once, and only after NetworkManager is started and GameSyncManager is spawned
        if (!_statesInitialized && _stateMachine != null && _syncManager != null && _syncManager.IsSpawned)
        {
            _statesInitialized = true;
            _stateMachine.SetState(_waitingState);
        }
    }

    private void OnDestroy()
    {
        if (_syncManager != null && _toothPickState != null)
            _syncManager.OnToothSelectionReceived -= _toothPickState.HandleToothSelection;

        // Unsubscribe from NetworkManager events
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}
}