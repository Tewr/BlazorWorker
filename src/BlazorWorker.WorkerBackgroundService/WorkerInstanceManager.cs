using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using BlazorWorker.WorkerCore.SimpleInstanceService;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
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
        private readonly SimpleInstanceService simpleInstanceService;

        public WorkerInstanceManager()
        {
            this.serializer = new DefaultMessageSerializer();
            this.options = new WebWorkerOptions();
            this.simpleInstanceService = SimpleInstanceService.Instance;

            this.messageHandlerRegistry = new MessageHandlerRegistry(this.serializer);
            this.messageHandlerRegistry.Add<InitInstance>(InitInstance);
            this.messageHandlerRegistry.Add<DisposeInstance>(DisposeInstance);
            this.messageHandlerRegistry.Add<MethodCallParams>(HandleMethodCall);
            this.messageHandlerRegistry.Add<RegisterEvent>(RegisterEvent);
            this.messageHandlerRegistry.Add<UnRegisterEvent>(UnRegisterEvent);
        }

        public static void Init() {
            MessageService.Message += Instance.OnMessage;
            Instance.PostObject(new InitWorkerComplete());
#if DEBUG
            Console.WriteLine($"BlazorWorker.WorkerBackgroundService.{nameof(WorkerInstanceManager)}.Init(): Done.");
#endif
        }

        public void PostMessage(string message)
        {
#if DEBUG
            Console.WriteLine($"BlazorWorker.WorkerBackgroundService.{nameof(WorkerInstanceManager)}.PostMessage(): {message}.");
#endif
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

            void handleError(Exception e)
            {
                PostObject(
                new MethodCallResult()
                {
                    CallId = methodCallMessage.CallId,
                    IsException = true,
                    Exception = e
                });
            }

            try
            {
                Task.Run(async () =>
                    await MethodCall(methodCallMessage))
                    .ContinueWith(t => { 
                        if (t.IsFaulted)
                        {
                            handleError(t.Exception);
                        }
                        else
                        {
                            PostObject(
                                new MethodCallResult
                                {
                                    CallId = methodCallMessage.CallId,
                                    ResultPayload = this.serializer.Serialize(t.Result)
                                }
                            );
                        }
                    });
            }
            catch (Exception e)
            {
                handleError(e);
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
            var instance = simpleInstanceService.instances[registerEventMessage.InstanceId].Instance;
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
            var initResult = simpleInstanceService.InitInstance(
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
            var res = simpleInstanceService.DisposeInstance(
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

        public async Task<object> MethodCall(MethodCallParams instanceMethodCallParams)
        {
            var instance = simpleInstanceService.instances[instanceMethodCallParams.InstanceId].Instance;
            var lambda = this.options.ExpressionSerializer.Deserialize(instanceMethodCallParams.SerializedExpression) 
                as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            var result = dynamicDelegate.DynamicInvoke(instance);
            
            if (!instanceMethodCallParams.AwaitResult)
            {
                return result;
            }
            
            var taskResult = result as Task;
            if (taskResult != null)
            {
                await taskResult;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected return type. Expected {nameof(Task)}, found '{result.GetType()}'");
            }

            var resultType = taskResult.GetType();
            if (!resultType.IsGenericType)
            {
                // Task without result
                return null;
            }

            // Task<T>
            return resultType.GetProperty(nameof(Task<object>.Result)).GetValue(result);
        }
    }
}
