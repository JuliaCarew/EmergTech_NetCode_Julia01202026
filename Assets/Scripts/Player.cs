
namespace CrocoType.Domain
{
    public class Player 
    {
        // identity
        public ulong ClientId { get; }
        public string PlayerName { get; }

        // ITypingParticipant
        public string CurrentInput { get; private set; } = "";
        public float CompletionTime { get; private set; } = -1f;
        public bool HasFinished { get; private set; }

        public void SubmitCharacter(char c)
        {
            if (HasFinished) return;
            CurrentInput += c;
        }

        public void ResetInput()
        {
            CurrentInput = "";
            CompletionTime = -1f;
            HasFinished = false;
        }

        public void MarkFinished(float completionTime)
        {
            if (HasFinished) return; // first finish wins
            HasFinished = true;
            CompletionTime = completionTime;
        }

        // IEliminable
        public bool IsAlive { get; private set; } = true;
        public int EliminatedOnRound { get; private set; } = -1;

        public void Eliminate(int round)
        {
            if (!IsAlive) return; // can only die once
            IsAlive = false;
            EliminatedOnRound = round;
        }

        // IToothSelector
        public int  SelectedToothIndex { get; private set; } = -1;
        public bool HasSelected => SelectedToothIndex >= 0;

        public void SelectTooth(int toothIndex)
        {
            if (HasSelected) return; // first pick locks in
            SelectedToothIndex = toothIndex;
        }

        public void ResetSelection()
        {
            SelectedToothIndex = -1;
        }

        // constructor
        public Player(ulong clientId, string playerName)
        {
            ClientId = clientId;
            PlayerName = playerName;
        }
    }
}
