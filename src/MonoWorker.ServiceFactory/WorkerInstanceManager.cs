using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using MonoWorker.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MonoWorker.BackgroundServiceHost
{
    public partial class WorkerInstanceManager
    {
        public readonly Dictionary<long, IEventWrapper> events =
             new Dictionary<long, IEventWrapper>();

        public static readonly WorkerInstanceManager Instance = new WorkerInstanceManager();
        internal readonly ISerializer serializer;
        private readonly WebWorkerOptions options;
        private readonly MessageHandlerRegistry messageHandlerRegistry;

        public WorkerInstanceManager()
        {
            this.serializer = new DefaultMessageSerializer();
            this.options = new WebWorkerOptions();

            this.messageHandlerRegistry = new MessageHandlerRegistry(this.serializer);
            this.messageHandlerRegistry.Add<InitInstanceParams>(InitInstance);
            this.messageHandlerRegistry.Add<MethodCallParams>(HandleMethodCall);
            this.messageHandlerRegistry.Add<RegisterEvent>(RegisterEvent);
            this.messageHandlerRegistry.Add<UnRegisterEvent>(UnRegisterEvent);
        }

        public static void Init() {
            MessageService.Message += Instance.OnMessage;
            Console.WriteLine("MonoWorker.BackgroundServiceHost.Init(): Done.");
            Instance.PostObject(new InitWorkerComplete());
        }

        public void PostMessage(string message)
        {
            Console.WriteLine($"MonoWorker.BackgroundServiceHost.PostMessage(): {message}.");
            MessageService.PostMessage(message);
        }

        internal void PostObject<T>(T obj)
        {
            PostMessage(this.serializer.Serialize(obj));
        }

        private bool IsInfrastructureMessage(string message)
        {
            return this.messageHandlerRegistry.HandlesMessage(message);
        }

        private void OnMessage(object sender, string message)
        {
            this.messageHandlerRegistry.HandleMessage(message);
        }

        private void HandleMethodCall(MethodCallParams methodCallMessage)
        {
            try
            {
                var result = MethodCall(methodCallMessage);
                PostObject(
                    new MethodCallResult()
                    {
                        CallId = methodCallMessage.CallId,
                        ResultPayload = this.serializer.Serialize(result)
                    }
                );
            }
            catch (Exception e)
            {
                PostObject(
                    new MethodCallResult()
                    {
                        CallId = methodCallMessage.CallId,
                        IsException = true,
                        Exception = e
                    });
            }
        }

        private void UnRegisterEvent(UnRegisterEvent unregisterEventMessage)
        {
            if (!events.TryGetValue(unregisterEventMessage.EventHandleId, out var wrapper)) {
                return;
            }

            wrapper.Unregister();

            events.Remove(unregisterEventMessage.EventHandleId);
        }

        private void RegisterEvent(RegisterEvent registerEventMessage)
        {
            var instance = SimpleInstanceService.Instance.instances[registerEventMessage.InstanceId].Instance;
            var eventSignature = instance.GetType().GetEvent(registerEventMessage.EventName);

            // TODO: This can be cached.
            var wrapperType = typeof(EventHandlerWrapper<>)
                .MakeGenericType(Type.GetType(registerEventMessage.EventHandlerTypeArg));
            
            var wrapper = (IEventWrapper)Activator.CreateInstance(wrapperType, this, registerEventMessage.InstanceId, registerEventMessage.EventHandleId);
            var delegateMethod = Delegate.CreateDelegate(eventSignature.EventHandlerType, wrapper, nameof(EventHandlerWrapper<object>.OnEvent)); 
            eventSignature.AddEventHandler(instance, delegateMethod);
            wrapper.Unregister = () => eventSignature.RemoveEventHandler(instance, delegateMethod);
            events.Add(wrapper.EventHandleId, wrapper);
        }

        public void InitInstance(InitInstanceParams createInstanceInfo)
        {
            //Console.WriteLine($"{nameof(WorkerInstanceManager)}.{nameof(InitInstance)}");
            var initResult = SimpleInstanceService.Instance.InitInstance(createInstanceInfo.InstanceId, createInstanceInfo.TypeName, createInstanceInfo.AssemblyName);            //Console.WriteLine($"{nameof(WorkerInstanceManager)}.{nameof(InitInstance)} done. {r.IsSuccess}:{r.FullExceptionString}");
            PostObject(new InitInstanceComplete() { 
                CallId = createInstanceInfo.CallId, 
                IsSuccess = initResult.IsSuccess, 
                Exception = initResult.Exception });
        }

        public object MethodCall(MethodCallParams instanceMethodCallParams)
        {
            var instance = SimpleInstanceService.Instance.instances[instanceMethodCallParams.InstanceId].Instance;
            var lambda = this.options.ExpressionSerializer.Deserialize(instanceMethodCallParams.SerializedExpression) 
                as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            return dynamicDelegate.DynamicInvoke(instance);
        }
    }

    public class EventHandlerWrapper<T> : IEventWrapper
    {
        private readonly WorkerInstanceManager wim;

        public EventHandlerWrapper(
            WorkerInstanceManager wim, 
            long instanceId, 
            long eventHandleId)
        {
            this.wim = wim;
            InstanceId = instanceId;
            EventHandleId = eventHandleId;
        }

        public long InstanceId { get; }
        public long EventHandleId { get; }

        public Action Unregister { get; set; }

        public void OnEvent(object _, T eventArgs)
        {
            //Console.WriteLine("ONEVENT");
            wim.PostObject(new EventRaised()
            {
                EventHandleId = EventHandleId,
                InstanceId = InstanceId,
                ResultPayload = wim.serializer.Serialize(eventArgs)
            });
        }
    }

    public interface IEventWrapper
    {
        long InstanceId { get; }
        long EventHandleId { get; }
        Action Unregister { get; set; }
    }

}
