using Serialize.Linq.Nodes;

namespace BlazorWorker.BackgroundServiceFactory.Shared
{
    public class InstanceMethodCallParams
    {
        public string InstanceId { get; set; }
        public ExpressionNode MethodCall { get; set; }
        public string WorkerId { get; set; }
    }
}
