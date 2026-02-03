using Unity.Netcode;
using UnityEngine;

namespace CrocoType.Networking
{
    public class PlayerNetworkState : NetworkBehaviour
    {
        // synced state
        public NetworkVariable<int> CorrectCharCount = new(0);
        public NetworkVariable<int> SelectedTooth = new(-1);
        public NetworkVariable<bool> IsAlive = new(true);
        public NetworkVariable<ulong> OwnerClientId = new(ulong.MaxValue);

        // setup
        public void InitializeOwner(ulong clientId)
        {
            OwnerClientId.Value = clientId;
        }

        // per-round reset (called by server)
        public void ResetForNewRound()
        {
            CorrectCharCount.Value = 0;
            SelectedTooth.Value    = -1;
        }

        // update helpers
        public void UpdateCorrectCharCount(int count)
        {
            CorrectCharCount.Value = count;
        }

        public void SetToothSelection(int toothIndex)
        {
            SelectedTooth.Value = toothIndex;
        }

        public void SetAlive(bool alive)
        {
            IsAlive.Value = alive;
        }
    }
}