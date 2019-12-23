using System;
using System.Threading.Tasks;

namespace MonoWorker.Core
{
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
