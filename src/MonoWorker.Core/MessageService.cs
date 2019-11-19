using System;

namespace MonoWorker.Core
{
    /// <summary>
    /// Simple static message service that runs in the worker thread.
    /// </summary>
    public class MessageService
    {
        //public static readonly WorkerInstanceManager Instance = new WorkerInstanceManager();
        private static readonly DOMObject self = new DOMObject("self");

        public static event EventHandler<string> Message;

        // todo: this string could/(should?) be a byte
        public static void OnMessage(string message)
        {
            Message?.Invoke(null, message);
            Console.WriteLine($"Worker.MessageHandler: {message}");
            SendMessage($"Worker.MessageHandler: ECHO {message}");
        }

        public static void SendMessage(string message)
        {
            self.Invoke("postMessage", message);
        }

        public static void Dispose()
        {
            self.Dispose();
        }

       /* public static void InitInstance(InitInstanceParams createInstanceInfo) 
            => Instance.InitInstance(createInstanceInfo);

        public static object Call(InstanceMethodCallParams instanceMethodCallParams)
           => Instance.Call(instanceMethodCallParams);*/
    }
}
