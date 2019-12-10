namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class EventRaised : BaseMessage
    {
        public EventRaised()
        {
            MessageType = nameof(EventRaised);
        }

        public long EventHandleId { get; }

        public string ResultPayload { get; set; }
    }
}
