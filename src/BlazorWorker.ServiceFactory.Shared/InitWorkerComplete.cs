namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitWorkerComplete : BaseMessage
    {
        public InitWorkerComplete()
        {
            MessageType = nameof(InitWorkerComplete);
        }
    }
}
