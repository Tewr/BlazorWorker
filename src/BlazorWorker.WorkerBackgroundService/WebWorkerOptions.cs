using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWorker.WorkerBackgroundService
{
    public class WebWorkerOptions
    {
        private readonly IEnumerable<Type> customKnownTypes;
        private ISerializer messageSerializer;
        private IExpressionSerializer expressionSerializer;

        public WebWorkerOptions(IEnumerable<string> customKnownTypeNames = null) =>
            customKnownTypes = customKnownTypeNames?.Select(name => Type.GetType(name));

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