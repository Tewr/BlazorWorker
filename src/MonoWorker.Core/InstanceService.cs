using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MonoWorker.Core
{
    public class SimpleInstanceService
    {
        public static readonly SimpleInstanceService Instance = new SimpleInstanceService();
        public readonly Dictionary<long, object> instances = new Dictionary<long, object>();

        public SimpleInstanceService()
        {
            
        }

        public static void Init()
        {
            Instance.
        }

        public void InitNewInstance(string initMessage)
        {
            var splitMessage = initMessage.Split('-');
            var id = long.Parse(splitMessage[0]);
            var typeName = splitMessage[1];
            var assemblyName = splitMessage[2];
            var type = Type.GetType($"{typeName}, {assemblyName}");

        }

        public class InjectableMessageService : IWorkerMessageService, IDisposable
        {
            public InjectableMessageService()
            {
                MessageService.Message += OnIncomingMessage;
            }

            private void OnIncomingMessage(object sender, string e)
            {
                if (e.StartsWith($"{nameof(SimpleInstanceService)}."))
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
