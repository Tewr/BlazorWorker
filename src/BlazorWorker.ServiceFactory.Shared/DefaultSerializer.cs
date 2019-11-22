using Newtonsoft.Json;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class DefaultSerializer : ISerializer
    {
        private static DefaultSerializer _instance;

        public static DefaultSerializer Instance => _instance ?? (_instance = new DefaultSerializer());

        public static JsonSerializerSettings settings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects
        };
        

        public T Deserialize<T>(string objStr)
        {
            return JsonConvert.DeserializeObject<T>(objStr);
        }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}
