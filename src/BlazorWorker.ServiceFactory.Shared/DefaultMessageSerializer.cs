using System.Text.Json;
using Newtonsoft.Json;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class DefaultMessageSerializer : ISerializer
    {
        /*public T Deserialize<T>(string objStr)
        {
            return JsonSerializer.Deserialize<T>(objStr);
        }

        public string Serialize(object obj)
        {
            return JsonSerializer.Serialize(obj);
        }*/

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

/*
using Newtonsoft.Json;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class DefaultMessageSerializer : ISerializer
    {
        private static DefaultMessageSerializer _instance;

        public static DefaultMessageSerializer Instance => _instance ?? (_instance = new DefaultMessageSerializer());

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
*/