using System;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstanceComplete : BaseMessage
    {
        public InitInstanceComplete()
        {
            MessageType = nameof(InitInstanceComplete);
        }

        public long CallId { get; set; }

        public bool IsSuccess { get; set; }

        public Exception Exception { get; set; }
    }
}
