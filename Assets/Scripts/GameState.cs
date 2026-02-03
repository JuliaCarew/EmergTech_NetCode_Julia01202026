using Unity.Netcode;
using CrocoType.States;
using CrocoType.Networking;

public abstract class GameState 
{
    protected readonly GameStateMachine StateMachine;
    protected readonly GameSyncManager  SyncManager;

    protected GameState(GameStateMachine stateMachine, GameSyncManager syncManager)
    {
        StateMachine = stateMachine;
        SyncManager  = syncManager;
    }
    public virtual void Enter() { }

    public virtual void Update() { }

    public virtual void Exit() { }

    protected void TransitionTo(GameState next)
    {
        if (!NetworkManager.Singleton.IsServer)
            return; 

        StateMachine.SetState(next);
    }
}
