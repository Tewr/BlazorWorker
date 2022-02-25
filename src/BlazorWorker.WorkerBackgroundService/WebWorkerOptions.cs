using System;

namespace BlazorWorker.WorkerBackgroundService
{
    public class WebWorkerOptions
    {
        private readonly Type[] customKnownTypes;
        private ISerializer messageSerializer;
        private IExpressionSerializer expressionSerializer;

        public WebWorkerOptions(Type[] customKnownTypes = null)
        {
            this.customKnownTypes = customKnownTypes;
        }

        public ISerializer MessageSerializer
        {
            get => messageSerializer ?? new DefaultMessageSerializer();
            set => messageSerializer = value;
        }

        public IExpressionSerializer ExpressionSerializer
        {
            get => expressionSerializer ?? new SerializeLinqExpressionSerializer(customKnownTypes);
            set => expressionSerializer = value;
        }
    }
}
