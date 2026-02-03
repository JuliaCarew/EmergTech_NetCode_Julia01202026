using System.Collections.Generic;
using UnityEngine;

namespace CrocoType.States
{
    public class GameStateMachine : MonoBehaviour
    {
        private readonly Dictionary<System.Type, GameState> _stateRegistry = new();

        private GameState _currentState;

        public void RegisterState(GameState state)
        {
            _stateRegistry[state.GetType()] = state;
        }

        private void Update()
        {
            _currentState?.Update();
        }

        public void SetState(GameState newState)
        {
            if (newState == null)
            {
                Debug.LogError("[GameStateMachine] Cannot transition to null.");
                return;
            }

            _currentState?.Exit();
            _currentState = newState;
            _currentState.Enter();
        }

        public T GetState<T>() where T : GameState
        {
            _stateRegistry.TryGetValue(typeof(T), out var state);
            return state as T;
        }

        public GameState CurrentState => _currentState;
    }
}