namespace BlazorWorker.Extensions.JSRuntime
{
    internal class DefaultBlazorWorkerJSRuntimeSerializer : IBlazorWorkerJSRuntimeSerializer
    {
        public T Deserialize<T>(string serializedObject)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(serializedObject);
        }

        public string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
    }
}
