using Serialize.Linq.Serializers;
using System;
using System.Linq.Expressions;

namespace BlazorWorker.WorkerBackgroundService
{
    public abstract class SerializeLinqExpressionJsonSerializerBase : IExpressionSerializer
    {
        private ExpressionSerializer serializer;

        private ExpressionSerializer Serializer
            => this.serializer ?? (this.serializer = GetSerializer());

        public bool? AutoAddKnownTypesAsArrayTypes { get; set; }
        public bool? AutoAddKnownTypesAsListTypes { get; set; }

        public abstract Type[] GetKnownTypes();

        private ExpressionSerializer GetSerializer()
        {
            var jsonSerializer = new JsonSerializer();
            foreach (var type in GetKnownTypes())
            {
                jsonSerializer.AddKnownType(type);
            }
            if (this.AutoAddKnownTypesAsArrayTypes.HasValue) {
                jsonSerializer.AutoAddKnownTypesAsArrayTypes = this.AutoAddKnownTypesAsArrayTypes.Value; 
            }
            if (this.AutoAddKnownTypesAsListTypes.HasValue)
            {
                jsonSerializer.AutoAddKnownTypesAsListTypes = this.AutoAddKnownTypesAsListTypes.Value;
            }

            return new ExpressionSerializer(jsonSerializer);
        }

        public Expression Deserialize(string expressionString)
        {
            return this.Serializer.DeserializeText(expressionString);
        }

        public string Serialize(Expression expression)
        {
            return this.Serializer.SerializeText(expression);
        }
    }
}
