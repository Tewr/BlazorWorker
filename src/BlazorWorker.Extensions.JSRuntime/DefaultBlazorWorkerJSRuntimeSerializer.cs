using Microsoft.JSInterop;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorWorker.Extensions.JSRuntime
{
    internal class DefaultBlazorWorkerJSRuntimeSerializer : IBlazorWorkerJSRuntimeSerializer
    {
        private readonly JsonSerializerOptions options;

        public DefaultBlazorWorkerJSRuntimeSerializer(IJSRuntime jSRuntime)
        {
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    { new DotNetObjectReferenceJsonConverterFactory(jSRuntime) }
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
