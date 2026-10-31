namespace Artmann.Terminal
{
    // Persists the command history as one newline-separated blob.
    public interface IHistoryStore
    {
        string Load();
        void Save(string blob);
        void Clear();
    }
}
