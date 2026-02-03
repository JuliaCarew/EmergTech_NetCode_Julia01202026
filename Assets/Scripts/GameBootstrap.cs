using UnityEngine;
using CrocoType.Domain;
using CrocoType.Networking;
using CrocoType.States;

namespace CrocoType
{
    public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameStateMachine _stateMachine;
    [SerializeField] private GameSyncManager  _syncManager;

    private void Awake()
    {
        // set up states

        // set & register states

    }

    private void OnDestroy()
    {

    }
}
}