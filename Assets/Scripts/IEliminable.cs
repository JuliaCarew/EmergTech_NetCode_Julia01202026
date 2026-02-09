namespace CrocoType.Interfaces
{
    public interface IEliminable
    {
        bool IsAlive { get; }

        int EliminatedOnRound { get; }

        void Eliminate(int round);
    }
}
