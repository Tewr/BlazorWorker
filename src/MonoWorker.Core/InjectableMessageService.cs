using System;
using System.Threading.Tasks;

namespace MonoWorker.Core
{
    public delegate bool IsInfrastructureMessage(string message);
    public class InjectableMessageService : IWorkerMessageService, IDisposable
    {
        private readonly IsInfrastructureMessage isInfrastructureMessage;

        public InjectableMessageService(IsInfrastructureMessage isInfrastructureMessage)
        {
            MessageService.Message += OnIncomingMessage;
            this.isInfrastructureMessage = isInfrastructureMessage;
        }

        private void OnIncomingMessage(object sender, string e)
        {
            if (isInfrastructureMessage(e))
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
