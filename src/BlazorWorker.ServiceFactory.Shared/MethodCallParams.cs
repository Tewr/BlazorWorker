using Serialize.Linq.Nodes;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class MethodCallParams : BaseMessage
    {
        public MethodCallParams()
        {
            MessageType = nameof(MethodCallParams);
        }

        public long InstanceId { get; set; }
        public string SerializedExpression { get; set; }
        public long WorkerId { get; set; }
        public long CallId { get; set; }
    }
}
