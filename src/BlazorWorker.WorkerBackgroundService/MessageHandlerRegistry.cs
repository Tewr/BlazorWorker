using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using System;
using System.Collections.Generic;

namespace BlazorWorker.WorkerBackgroundService
{
    public class MessageHandlerRegistry : Dictionary<string, Action<string>>
    {
        public MessageHandlerRegistry(ISerializer messageSerializer)
        {
            MessageSerializer = messageSerializer;
        }

        public ISerializer MessageSerializer { get; }

        public void Add<T>(Action<T> messageHandler) where T : BaseMessage
        {
            base.Add(typeof(T).Name, message => messageHandler(MessageSerializer.Deserialize<T>(message)));
        }

        public bool HandleMessage(string message)
        {
            if (base.TryGetValue(GetMessageType(message), out var handler))
            {
                handler(message);
                return true;
            }

            return false;
        }

        public bool HandlesMessage(string message)
        {
            var key = GetMessageType(message);
            return base.ContainsKey(key);
        }

        private string GetMessageType(string message)
        {
            return this.MessageSerializer.Deserialize<BaseMessage>(message).MessageType;
        }
    }
}

