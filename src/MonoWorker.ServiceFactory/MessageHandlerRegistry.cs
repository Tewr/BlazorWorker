using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using System;
using System.Collections.Generic;

namespace MonoWorker.BackgroundServiceHost
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
            if (this.TryGetValue(GetMessageType(message), out var handler))
            {
                handler(message);
                return true;
            }

            return false;
        }

        public bool HandlesMessage(string message)
        {
            return this.ContainsKey(GetMessageType(message));
        }

        private string GetMessageType(string message)
        {
            return this.MessageSerializer.Deserialize<BaseMessage>(message).MessageType;
        }
    }
}

