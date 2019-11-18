namespace BlazorWorker.BackgroundServiceFactory
{
    public class WebWorkerOptions
    {
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
    }
}
