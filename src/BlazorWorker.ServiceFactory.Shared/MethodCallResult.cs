namespace BlazorWorker.BackgroundServiceFactory.Shared
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

    public class InitInstanceParamsResult : MethodCallResult
    {
        public InitInstanceParamsResult()
        {
            MessageType = nameof(InitInstanceParamsResult);
        }
    }
}
