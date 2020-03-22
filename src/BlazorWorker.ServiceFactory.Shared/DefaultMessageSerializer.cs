using Newtonsoft.Json;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class DefaultMessageSerializer : ISerializer
    {
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
