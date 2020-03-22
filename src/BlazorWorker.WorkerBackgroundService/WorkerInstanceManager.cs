using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.WorkerCore;
using BlazorWorker.WorkerCore.SimpleInstanceService;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerBackgroundService
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
            this.messageHandlerRegistry.Add<InitInstance>(InitInstance);
            this.messageHandlerRegistry.Add<DisposeInstance>(DisposeInstance);
            this.messageHandlerRegistry.Add<MethodCallParams>(HandleMethodCall);
            this.messageHandlerRegistry.Add<RegisterEvent>(RegisterEvent);
            this.messageHandlerRegistry.Add<UnRegisterEvent>(UnRegisterEvent);
        }

        public static void Init() {
            MessageService.Message += Instance.OnMessage;
            Console.WriteLine("BlazorWorker.WorkerBackgroundService.Init(): Done.");
            Instance.PostObject(new InitWorkerComplete());
        }

        public void PostMessage(string message)
        {
            Console.WriteLine($"BlazorWorker.WorkerBackgroundService.PostMessage(): {message}.");
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

        public void InitInstance(InitInstance createInstanceInfo)
        {
            var initResult = SimpleInstanceService.Instance.InitInstance(
                new InitInstanceRequest
                {
                    Id = createInstanceInfo.InstanceId,
                    TypeName = createInstanceInfo.TypeName,
                    AssemblyName = createInstanceInfo.AssemblyName
                }, IsInfrastructureMessage);            
            
            PostObject(new InitInstanceComplete() { 
                CallId = createInstanceInfo.CallId, 
                IsSuccess = initResult.IsSuccess, 
                Exception = initResult.Exception,
            });
        }

        public void DisposeInstance(DisposeInstance dispose)
        {
            var res = SimpleInstanceService.Instance.DisposeInstance(
                new DisposeInstanceRequest { 
                    InstanceId = dispose.InstanceId,
                    CallId = dispose.CallId
                });

            PostObject(new DisposeInstanceComplete
            {
                CallId = res.CallId,
                IsSuccess = res.IsSuccess,
                Exception = res.Exception
            });
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
}
