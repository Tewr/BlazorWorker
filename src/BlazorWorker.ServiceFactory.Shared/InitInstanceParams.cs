using System.Net.WebSockets;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstanceParams : BaseMessage
    {
        public InitInstanceParams()
        {
            MessageType = nameof(InitInstanceParams);
        }

        public long WorkerId { get; set; }
        public long InstanceId { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }

        public long CallId { get; set; }
    }
}
