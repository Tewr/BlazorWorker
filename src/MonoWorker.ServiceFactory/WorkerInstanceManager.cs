using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.Core;
using MonoWorker.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace MonoWorker.BackgroundServiceHost
{
    public class WorkerInstanceManager : IWorkerMessageService
    {
        public readonly Dictionary<long, object> instances = new Dictionary<long, object>();

        public static readonly WorkerInstanceManager Instance = new WorkerInstanceManager();
        private readonly ISerializer serializer;
        private readonly WebWorkerOptions options;

        public event EventHandler<string> IncomingMessage;

        public WorkerInstanceManager()
        {
            this.serializer = new DefaultMessageSerializer();
            this.options = new WebWorkerOptions();
        }

        public static void Init() {
            MessageService.Message += Instance.OnMessage;
            Console.WriteLine("MonoWorker.BackgroundServiceHost.Init(): Done.");
            Instance.PostObjecAsync(new InitWorkerComplete());
        }

        public async Task PostMessageAsync(string message)
        {
            Console.WriteLine($"MonoWorker.BackgroundServiceHost.PostMessage(): {message}.");
            MessageService.PostMessage(message);
        }

        private async Task PostObjecAsync<T>(T obj)
        {
            await PostMessageAsync(this.serializer.Serialize(obj));
        }

        private void OnMessage(object sender, string message)
        {
            var baseMessage = this.serializer.Deserialize<BaseMessage>(message);
            if (baseMessage.MessageType == nameof(InitInstanceParams))
            {
                var initMessage = this.serializer.Deserialize<InitInstanceParams>(message);
                InitInstance(initMessage);
                return;
            }

            if (baseMessage.MessageType == nameof(MethodCallParams))
            {
                var methodCallMessage = this.serializer.Deserialize<MethodCallParams>(message);
                try
                {
                    var result = Call(methodCallMessage);
                    PostMessageAsync(
                        this.serializer.Serialize(
                               new MethodCallResult()
                               {
                                   CallId = methodCallMessage.CallId,
                                   ResultPayload = this.serializer.Serialize(result)
                               }
                    ));
                }
                catch (Exception e)
                {
                    PostMessageAsync(this.serializer.Serialize(
                        new MethodCallResult()
                        {
                            CallId = methodCallMessage.CallId,
                            IsException = true,
                            Exception = e
                        }));
                }
                return;
            }

            if (baseMessage.MessageType == nameof(RegisterEvent))
            {
                var registerEventMessage = this.serializer.Deserialize<RegisterEvent>(message);
                RegisterEvent(registerEventMessage);
            }

            IncomingMessage?.Invoke(this, message);
        }

        private void RegisterEvent(RegisterEvent registerEventMessage)
        {
            var instance = instances[registerEventMessage.InstanceId];
            var eventSignature = instance.GetType().GetEvent(registerEventMessage.EventName);
            var delegateMethod = Delegate.CreateDelegate(this.GetType(), this.GetType().GetMethod(registerEventMessage.EventName));
            eventSignature.AddEventHandler(instance, delegateMethod);
        }

        private void OnEvent(object source, object eventArgs) { 
        }

        public void InitInstance(InitInstanceParams createInstanceInfo)
        {
            Type type;
            try
            {
                type = Type.GetType($"{createInstanceInfo.TypeName}, {createInstanceInfo.AssemblyName}");
            }
            catch (Exception e)
            {
                throw new InitWorkerInstanceException($"Unable to to load type {createInstanceInfo.TypeName} from {createInstanceInfo.AssemblyName}", e);
            }

            //TODO: inject message service here if applicable
            try
            {
                instances[createInstanceInfo.InstanceId] = Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                throw new InitWorkerInstanceException($"Unable to to instanciate type {createInstanceInfo.TypeName} from {createInstanceInfo.AssemblyName}", e);
            }

            Instance.PostObjecAsync(
                new InitInstanceComplete() { 
                    CallId = createInstanceInfo.CallId 
                });
        }

        public object Call(MethodCallParams instanceMethodCallParams)
        {
            var instance = instances[instanceMethodCallParams.InstanceId];
            var lambda = this.options.ExpressionSerializer.Deserialize(instanceMethodCallParams.SerializedExpression) 
                as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            return dynamicDelegate.DynamicInvoke(instance);
        }
    }
}
