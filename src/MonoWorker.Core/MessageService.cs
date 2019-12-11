using System;

namespace MonoWorker.Core
{
    /// <summary>
    /// Simple static message service that runs in the worker thread.
    /// </summary>
    public class MessageService
    {
        private static readonly DOMObject self = new DOMObject("self");

        public static event EventHandler<string> Message;

        static MessageService()
        {
            Console.WriteLine("MessageService static constructor");
            try
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in loadedAssemblies)
                {
                    Console.WriteLine($"assembly: {asm.FullName}");
                }
                var wim = Type.GetType($"MonoWorker.BackgroundServiceHost.WorkerInstanceManager, MonoWorker.BackgroundServiceHost");
                var obj = Activator.CreateInstance(wim);

            }
            catch (Exception e)
            {
                Console.WriteLine("MessageService static constructor fail:" + e.ToString());
            }
            
        }

        public static void OnMessage(string message)
        {
            Message?.Invoke(null, message);
            Console.WriteLine($"{nameof(MessageService)}.{nameof(OnMessage)}: {message}");
        }

        public static void PostMessage(string message)
        {
            self.Invoke("postMessage", message);
        }

        public static void Dispose()
        {
            self.Dispose();
        }

        public static object Init()
        {
            throw new NotImplementedException();
        }
    }
}
