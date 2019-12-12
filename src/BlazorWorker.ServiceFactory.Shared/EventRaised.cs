namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class EventRaised : BaseMessage
    {
        public EventRaised()
        {
            MessageType = nameof(EventRaised);
        }

        public long EventHandleId { get; set; }

        public long InstanceId { get; set; }

        public string ResultPayload { get; set; }
    }
}
