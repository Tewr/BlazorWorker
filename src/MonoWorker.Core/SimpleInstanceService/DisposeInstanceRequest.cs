using System;
using System.Collections.Generic;
using System.Text;

namespace MonoWorker.Core.SimpleInstanceService
{
    public class DisposeInstanceRequest
    {
        public readonly static string Prefix = 
            $"{SimpleInstanceService.MessagePrefix}{SimpleInstanceService.DiposeMessagePrefix}";

        public long CallId { get; set; }

        public long InstanceId { get; set; }

        public static bool CanDeserialize(string message)
        {
            return message.StartsWith(Prefix);
        }

        public static DisposeInstanceRequest Deserialize(string message)
        {
            var result = new DisposeInstanceRequest();
            var parsers = new Queue<Action<string>>(
                new Action<string>[]
                {
                    s => result.CallId = long.Parse(s),
                    s => result.InstanceId = long.Parse(s)
                });
            
            CSVDeserializer.Deserialize(message, Prefix, parsers);
            
            return result;
        }

        public string Serialize()
        {
            return Prefix + string.Join("|", new object[] { CallId, InstanceId });
        }
    }
}
