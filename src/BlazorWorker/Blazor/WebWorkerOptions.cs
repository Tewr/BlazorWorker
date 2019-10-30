namespace BlazorWorker.Blazor
{
    public class WebWorkerOptions
    {
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
    }
}
