namespace BlazorWorker.WorkerBackgroundService
{
    public class MethodCallResult : BaseMessage
    {
        public MethodCallResult()
        {
            MessageType = nameof(MethodCallResult);
        }

        public string ResultPayload { get; set; }

        public bool IsException { get; set; }

        public System.Exception Exception { get; set; }

        public long CallId { get; set; }
    }
}
