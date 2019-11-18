namespace BlazorWorker.BackgroundServiceFactory
{
    public interface ISerializer
    {
        string Serialize(object obj);

        T Deserialize<T>(string objStr);
    }
}
