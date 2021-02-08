using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    /// <summary>
    /// Simple static message service that runs in the worker thread.
    /// </summary>
    public class MessageService
    {
        private static readonly DOMObject self = DOMObject.Self;

        public static event EventHandler<string> Message;

        static MessageService()
        {   
        }

        public static void OnMessage(string message)
        {
            Message?.Invoke(null, message);
#if DEBUG
            Console.WriteLine($"{nameof(MessageService)}.{nameof(OnMessage)}: {message}");
#endif
        }

        public static void PostMessage(string message)
        {
            self.Invoke("postMessage", message);
        }

    }
}
