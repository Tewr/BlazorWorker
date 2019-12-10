namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstanceComplete : BaseMessage
    {
        public InitInstanceComplete()
        {
            MessageType = nameof(InitInstanceComplete);
        }

        public long CallId { get; set; }
    }
}
