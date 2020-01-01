using System;
using System.Collections.Generic;
using System.Text;

namespace MonoWorker.Core.SimpleInstanceService
{
    public class InitInstanceRequest
    {

        public static readonly string Prefix = $"{SimpleInstanceService.MessagePrefix}{SimpleInstanceService.InitMessagePrefix}";
        public long CallId { get; set; }
        public long Id { get; set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }

        internal static bool CanDeserialize(string initMessage)
        {
            return initMessage.StartsWith(Prefix);
        }

        internal static InitInstanceRequest Deserialize(string initMessage)
        {
            var splitMessage = initMessage.Substring(Prefix.Length).Split('|');
            var callId = long.Parse(splitMessage[0]);
            var id = long.Parse(splitMessage[1]);
            var typeName = splitMessage[2];
            var assemblyName = splitMessage[3];

            return new InitInstanceRequest
            {
                CallId = callId,
                Id = id,
                TypeName = typeName,
                AssemblyName = assemblyName
            };
        }

        public string Serialize()
        {
            return Prefix + string.Join("|", new object[] { CallId, Id, TypeName, AssemblyName });
        }
    }
}
