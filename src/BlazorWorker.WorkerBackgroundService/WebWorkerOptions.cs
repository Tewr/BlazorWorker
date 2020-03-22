namespace BlazorWorker.WorkerBackgroundService
{
    public class WebWorkerOptions
    {
        private ISerializer messageSerializer;
        private IExpressionSerializer expressionSerializer;

        public ISerializer MessageSerializer { 
            get => messageSerializer ?? (messageSerializer = new DefaultMessageSerializer()); 
            set => messageSerializer = value; 
        }

        public IExpressionSerializer ExpressionSerializer { 
            get => expressionSerializer ?? (expressionSerializer = new SerializeLinqExpressionSerializer()) ; 
            set => expressionSerializer = value; 
        }
    }
}
