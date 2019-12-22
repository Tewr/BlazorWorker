using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MonoWorker.Core
{
    public class SimpleInstanceService
    {
        public static readonly SimpleInstanceService Instance = new SimpleInstanceService();
        public readonly Dictionary<long, object> instances = new Dictionary<long, object>();
        public static readonly string MessagePrefix = $"{typeof(SimpleInstanceService).FullName}::";
        public static readonly string InitMessagePrefix = $"{nameof(InitInstance)}::";

        public static void Init()
        {
            Instance.InnerInit();
        }

        private void InnerInit()
        {
            MessageService.Message += OnMessage;
        }

        private void OnMessage(object sender, string rawMessage)
        {
            if (rawMessage.StartsWith(MessagePrefix) == false)
            {
                return;
            }

            rawMessage = rawMessage.Substring(MessagePrefix.Length);

            if (rawMessage.StartsWith(InitMessagePrefix)) {
                rawMessage = rawMessage.Substring(InitMessagePrefix.Length);
                InitInstance(rawMessage);
                return;
            }
        }

        public void InitInstance(string initMessage)
        {
            var splitMessage = initMessage.Split(';');
            var id = long.Parse(splitMessage[0]);
            var typeName = splitMessage[1];
            var assemblyName = splitMessage[2];

            var type = Type.GetType($"{typeName}, {assemblyName}");
            var constructors = type.GetConstructors();
            ConstructorInfo constructorInfo;
            var lastMatchArgCount = -1;
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length == 0 && lastMatchArgCount < 0)
                {
                    lastMatchArgCount = 0;
                    constructorInfo = constructor;
                    continue;
                }

                if (parameters.Length == 1 && lastMatchArgCount < 1)
                {
                    if (parameters[0].ParameterType == typeof(IWorkerMessageService))
                    {
                        lastMatchArgCount = 1;
                        constructorInfo = constructor;
                        continue;
                    }
                }
            }
            
            if (lastMatchArgCount == 0)
            {
                instances[id] = Activator.CreateInstance(type);
            }
            else if (lastMatchArgCount == 1)
            {
                instances[id] = Activator.CreateInstance(type, new InjectableMessageService());
            }
            
        }

        public class InjectableMessageService : IWorkerMessageService, IDisposable
        {
            public InjectableMessageService()
            {
                MessageService.Message += OnIncomingMessage;
            }

            private void OnIncomingMessage(object sender, string e)
            {
                if (e.StartsWith(SimpleInstanceService.MessagePrefix))
                {
                    return;
                }

                IncomingMessage?.Invoke(sender, e);
            }

            public event EventHandler<string> IncomingMessage;

            public void Dispose()
            {
                MessageService.Message -= OnIncomingMessage;
            }

            public async Task PostMessageAsync(string message)
            {
                await Task.Run(() => MessageService.PostMessage(message));
            }
        }
    }
}
