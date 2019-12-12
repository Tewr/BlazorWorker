using System;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class RegisterEvent: BaseMessage
    {
        public RegisterEvent()
        {
            MessageType = nameof(RegisterEvent);
        }

        public long EventHandleId { get; set; }

        public long InstanceId { get; set; }
        public string EventName { get; set; }
        public string EventHandlerTypeArg { get; set; }
    }
}
