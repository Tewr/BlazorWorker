using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorWorker.Extensions.JSRuntime
{
    internal class DefaultBlazorWorkerJSRuntimeSerializer : IBlazorWorkerJSRuntimeSerializer
    {
        private JsonSerializerOptions options;

        public DefaultBlazorWorkerJSRuntimeSerializer()
        {
            options = new JsonSerializerOptions
            {
                Converters = {
                    { new DotNetObjectReferenceJsonConverterFactory() }
                }
            };
        }
         

        public T Deserialize<T>(string serializedObject)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(serializedObject, options);
        }

        public string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, options);
        }
    }
}
