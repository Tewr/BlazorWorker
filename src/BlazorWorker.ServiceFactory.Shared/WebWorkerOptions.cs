namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class WebWorkerOptions
    {
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
    }
}
