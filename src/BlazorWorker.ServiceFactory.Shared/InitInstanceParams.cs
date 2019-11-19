using System.Net.WebSockets;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InitInstanceParams
    {
        public string MessageType => nameof(InitInstanceParams);

        public long WorkerId { get; set; }
        public string InstanceId { get; set; }
        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
    }
}
