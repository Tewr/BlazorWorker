using System;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore.SimpleInstanceService
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

        private void OnIncomingMessage(object sender, string rawMessage)
        {
            if (isInfrastructureMessage(rawMessage))
            {
                // Prevents Infrastructure messages from propagating downwards
                return;
            }

            IncomingMessage?.Invoke(sender, rawMessage);
        }

        public event EventHandler<string> IncomingMessage;

        public void Dispose()
        {
            MessageService.Message -= OnIncomingMessage;
        }

        public async Task PostMessageAsync(string message)
        {
            Console.WriteLine($"{nameof(InjectableMessageService)}.{nameof(PostMessageAsync)}('{message}')");
            MessageService.PostMessage(message);
        }
    }
}
