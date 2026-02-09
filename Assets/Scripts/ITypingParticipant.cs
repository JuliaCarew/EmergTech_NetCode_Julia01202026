namespace CrocoType.Interfaces
{
    public interface ITypingParticipant
    {
        ulong ClientId { get; }
        string PlayerName { get; }
        string CurrentInput { get; }
        float CompletionTime { get; }
        bool HasFinished { get; }

        void SubmitCharacter(char c);
        void ResetInput();
        void MarkFinished(float completionTime);
    }
}
