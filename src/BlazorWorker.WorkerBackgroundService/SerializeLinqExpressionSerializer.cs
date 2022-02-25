using Serialize.Linq.Serializers;
using System;
using System.Linq.Expressions;

namespace BlazorWorker.WorkerBackgroundService
{
    public class SerializeLinqExpressionSerializer : IExpressionSerializer
    {
        private readonly ExpressionSerializer serializer;
        
        public SerializeLinqExpressionSerializer(Type[] customKnownTypes = null)
        {
            var jsonSerializer = new JsonSerializer();

            if (customKnownTypes != null)
            {
                jsonSerializer.AddKnownTypes(customKnownTypes);
            }

            this.serializer = new ExpressionSerializer(jsonSerializer);
        }

        public Expression Deserialize(string expressionString)
        {
            return serializer.DeserializeText(expressionString);
        }

        public string Serialize(Expression expression)
        {
            return serializer.SerializeText(expression);
        }
    }
}
