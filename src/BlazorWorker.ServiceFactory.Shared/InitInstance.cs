using System.Net.WebSockets;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstance : BaseMessage
    {
        public InitInstance()
        {
            MessageType = nameof(InitInstance);
        }

        public long WorkerId { get; set; }
        public long InstanceId { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }

        public long CallId { get; set; }
    }
}
