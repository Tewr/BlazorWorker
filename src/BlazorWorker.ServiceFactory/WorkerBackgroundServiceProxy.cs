using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class WorkerBackgroundServiceProxy<T> : IWorkerBackgroundService<T> where T : class
    {
        private readonly IWorker worker;
        private readonly WebWorkerOptions options;
        private static readonly string InitEndPoint;
        private static long idSource;
        private long instanceId;
        private ISerializer messageSerializer;
        private object expressionSerializer;
        private MessageHandlerRegistry messageHandlerRegistry;
        private TaskCompletionSource<bool> initTask;
        private TaskCompletionSource<bool> disposeTask;
        private TaskCompletionSource<bool> initWorkerTask;
        // This doesnt really need to be static but easier to debug if messages have application-wide unique ids
        private static long messageRegisterIdSource;
        private readonly Dictionary<long, TaskCompletionSource<MethodCallResult>> messageRegister
            = new Dictionary<long, TaskCompletionSource<MethodCallResult>>();

        private Dictionary<long, EventHandle> eventRegister
            = new Dictionary<long, EventHandle>();

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        static WorkerBackgroundServiceProxy()
        {
            var wim = typeof(WorkerInstanceManager);
            InitEndPoint = $"[{wim.Assembly.GetName().Name}]{wim.FullName}:{nameof(WorkerInstanceManager.Init)}";
        }

        internal WorkerBackgroundServiceProxy(
            IWorker worker,
            WebWorkerOptions options)
        {
            this.worker = worker;
            this.options = options;
            this.instanceId = ++idSource;
            this.messageSerializer = this.options.MessageSerializer;
            this.expressionSerializer = this.options.ExpressionSerializer;

            this.messageHandlerRegistry = new MessageHandlerRegistry(this.options.MessageSerializer);
            this.messageHandlerRegistry.Add<InitInstanceComplete>(OnInitInstanceComplete);
            this.messageHandlerRegistry.Add<InitWorkerComplete>(OnInitWorkerComplete);
            this.messageHandlerRegistry.Add<DisposeInstanceComplete>(OnDisposeInstanceComplete);
            this.messageHandlerRegistry.Add<EventRaised>(OnEventRaised);
            this.messageHandlerRegistry.Add<MethodCallResult>(OnMethodCallResult);
        }

        private void OnDisposeInstanceComplete(DisposeInstanceComplete message)
        {
            if (message.IsSuccess)
            {
                this.disposeTask.SetResult(true);
                this.IsDisposed = true;
            }
            else
            {
                this.disposeTask.SetException(message.Exception);
            }
        }

        private bool IsInfrastructureMessage(string message)
        {
            return this.messageHandlerRegistry.HandlesMessage(message);
        }

        public IWorkerMessageService GetWorkerMessageService()
        {
            return this.worker;
        }

        public async Task InitAsync(WorkerInitOptions workerInitOptions = null)
        {
            workerInitOptions = workerInitOptions ?? new WorkerInitOptions();
            if (this.initTask != null)
            {
                await initTask.Task;
            }

            if (this.IsInitialized)
            {
                return;
            }

            initTask = new TaskCompletionSource<bool>();

            if (!this.worker.IsInitialized)
            {
                initWorkerTask = new TaskCompletionSource<bool>();
                await this.worker.InitAsync(new WorkerInitOptions() { 
                    DependentAssemblyFilenames = new[] { 
                        $"{typeof(BaseMessage).Assembly.GetName().Name}.dll",
                        $"{typeof(WorkerInstanceManager).Assembly.GetName().Name}.dll",
                        $"{typeof(Newtonsoft.Json.JsonConvert).Assembly.GetName().Name}.dll",
                        $"{typeof(IWorkerMessageService).Assembly.GetName().Name}.dll",
                        "System.Xml.dll",
                        "Serialize.Linq.dll",
                        "System.dll",
                        "System.Buffers.dll",
                        "System.Data.dll",
                        "System.Core.dll",
                        "System.Memory.dll",
                        "System.Numerics.dll",
                        "System.Numerics.Vectors.dll",
                        "System.Runtime.CompilerServices.Unsafe.dll",
                        "System.Runtime.Serialization.dll",
                        $"{typeof(System.Reflection.Assembly).Assembly.GetName().Name}.dll",
                        "Microsoft.Bcl.AsyncInterfaces.dll",
                        "System.Threading.Tasks.Extensions.dll",
                        "Mono.Security.dll",
                        "System.ServiceModel.Internals.dll"
                    },
                    InitEndPoint = InitEndPoint
                }.MergeWith(workerInitOptions));

                this.worker.IncomingMessage += OnMessage;
                await initWorkerTask.Task;
            }

            var message = this.options.MessageSerializer.Serialize(
                    new InitInstance()
                    {
                        WorkerId = this.worker.Identifier, // TODO: This should not really be necessary?
                        InstanceId = instanceId,
                        AssemblyName = typeof(T).Assembly.FullName,
                        TypeName = typeof(T).FullName
                    });
            Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}.InitAsync(): {this.worker.Identifier} {message}");

            await this.worker.PostMessageAsync(message);
            await initTask.Task;
        }

        private void OnMessage(object sender, string rawMessage)
        {
            this.messageHandlerRegistry.HandleMessage(rawMessage);
        }

        private void OnMethodCallResult(MethodCallResult message)
        {
            if (!this.messageRegister.TryGetValue(message.CallId, out var taskCompletionSource))
            {
                return;
            }

            taskCompletionSource.SetResult(message); 
            this.messageRegister.Remove(message.CallId);
        }

        private void OnEventRaised(EventRaised message)
        {
            if (!this.eventRegister.TryGetValue(message.EventHandleId, out var eventHandle))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(EventRaised)}: Unknown event id {message.EventHandleId}");
                return;
            }

            OnEventRaised(eventHandle, message.ResultPayload);
        }

        private void OnInitWorkerComplete(InitWorkerComplete message)
        {
            this.initWorkerTask.SetResult(true);
        }

        private void OnInitInstanceComplete(InitInstanceComplete message)
        {
            if (message.IsSuccess)
            {
                this.initTask.SetResult(true);
                this.IsInitialized = true;
            }
            else
            {
                this.initTask.SetException(message.Exception);
            }
        }

        private void OnEventRaised(EventHandle eventHandle, string eventPayload)
        {
            eventHandle.EventHandler.Invoke(eventPayload);
        }

        public async Task RunAsync(Expression<Action<T>> action)
        {
            await InvokeAsyncInternal<object>(action);
        }

        public async Task<TResult> RunAsync<TResult>(Expression<Func<T, TResult>> action)
        {
            return await InvokeAsyncInternal<TResult>(action);
        }

        public async Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler)
        {
            var eventSignature = typeof(T).GetEvent(eventName ?? throw new ArgumentNullException(nameof(eventName)));
            if (eventSignature == null)
            {
                throw new ArgumentException($"Type '{typeof(T).FullName}' does not expose any event named '{eventName}'");
            }

            if (!eventSignature.EventHandlerType.IsAssignableFrom(typeof(EventHandler<TResult>))){
                throw new ArgumentException($"Event '{typeof(T).FullName}.{eventName}' can only be assigned an event listener of type {typeof(EventHandler<TResult>).FullName}");
            }

            var handle = new EventHandle() { 
                EventHandler = 
                payload => {
                    var typedPayload = this.messageSerializer.Deserialize<TResult>(payload);
                    myHandler.Invoke(null, typedPayload); 
                } 
            };

            this.eventRegister.Add(handle.Id, handle);
            var message = new RegisterEvent() {
                EventName = eventName,
                EventHandlerTypeArg = typeof(TResult).FullName,
                EventHandleId = handle.Id,
                InstanceId = this.instanceId
            };
            var serializedMessage = this.options.MessageSerializer.Serialize(message);
            await this.worker.PostMessageAsync(serializedMessage);
            return handle;
        }

        public async Task UnRegisterEventListenerAsync(EventHandle handle)
        {
            if (handle is null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            var message = new UnRegisterEvent
            {
                EventHandleId = handle.Id
            };
            var serializedMessage = this.options.MessageSerializer.Serialize(message);
            await this.worker.PostMessageAsync(serializedMessage);
        }

        private async Task<TResult> InvokeAsyncInternal<TResult>(Expression action)
        {
            // If Blazor ever gets multithreaded this would need to be locked for race conditions
            // However, when/if that happens, most of this project is obsolete anyway
            var id = ++messageRegisterIdSource;
            var taskCompletionSource = new TaskCompletionSource<MethodCallResult>();
            this.messageRegister.Add(id, taskCompletionSource);
            var methodCall = GetCall(action, id);

            await this.worker.PostMessageAsync(methodCall);

            var returnMessage = await taskCompletionSource.Task;
            if (returnMessage.IsException)
            {
                throw new AggregateException($"Worker exception: {returnMessage.Exception.Message}",  returnMessage.Exception);
            }
            if (string.IsNullOrEmpty(returnMessage.ResultPayload))
            {
                return default;
            }

            return this.options.MessageSerializer.Deserialize<TResult>(returnMessage.ResultPayload);
        }

        private string GetCall(Expression action, long id)
        {

            var expression = this.options.ExpressionSerializer.Serialize(action);
            var methodCall = new MethodCallParams
            {
                WorkerId = this.worker.Identifier,
                InstanceId = instanceId,
                SerializedExpression = expression,
                CallId = id
            };

            return this.options.MessageSerializer.Serialize(methodCall);
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposeTask != null)
            {
                await disposeTask.Task;
            }

            if (this.IsDisposed)
            {
                return;
            }

            disposeTask = new TaskCompletionSource<bool>();

            var message = this.options.MessageSerializer.Serialize(
                   new DisposeInstance
                   {
                        InstanceId = instanceId,
                   });

            await this.worker.PostMessageAsync(message);

            await disposeTask.Task;
        }
    }
}
