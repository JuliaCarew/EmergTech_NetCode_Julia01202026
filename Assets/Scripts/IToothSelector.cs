namespace CrocoType.Interfaces
{
    public interface IToothSelector
    {
        int SelectedToothIndex { get; }
        bool HasSelected { get; }

        void SelectTooth(int toothIndex);
        void ResetSelection();
    }
}