using Serialize.Linq.Nodes;

namespace BlazorWorker.Shared
{
    public class InstanceMethodCallParams
    {
        public string InstanceId { get; set; }
        public ExpressionNode MethodCall { get; internal set; }
        public string WorkerId { get; internal set; }
    }
}
