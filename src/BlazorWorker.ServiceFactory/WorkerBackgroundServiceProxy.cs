using Microsoft.JSInterop;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using BlazorWorker.BackgroundServiceFactory.Shared;
using BlazorWorker.Core;
using System.Collections.Generic;
using MonoWorker.BackgroundServiceHost;
using MonoWorker.Core;

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
        private TaskCompletionSource<bool> initTask;
        private TaskCompletionSource<bool> initWorkerTask;
        // This doesnt really need to be static but easier to debug if messages have application-wide unique ids
        private static long messageRegisterIdSource;
        private Dictionary<long, TaskCompletionSource<MethodCallResult>> messageRegister
            = new Dictionary<long, TaskCompletionSource<MethodCallResult>>();

        private Dictionary<long, EventHandle> eventRegister
    = new Dictionary<long, EventHandle>();

        public event EventHandler<string> Message;

        public bool IsInitialized { get; private set; }

        static WorkerBackgroundServiceProxy()
        {
            var workerInitMethod = typeof(WorkerInstanceManager);
            InitEndPoint = $"[{workerInitMethod.Assembly.GetName().Name}]{workerInitMethod.FullName}:{nameof(WorkerInstanceManager.Init)}";
        }

        public WorkerBackgroundServiceProxy(
            IWorker worker,
            WebWorkerOptions options)
        {
            this.worker = worker;
            this.options = options;
            this.instanceId = ++idSource;
            this.messageSerializer = this.options.MessageSerializer;
            this.expressionSerializer = this.options.ExpressionSerializer;
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
                        $"{this.GetType().Assembly.GetName().Name}.dll",
                        $"{typeof(BaseMessage).Assembly.GetName().Name}.dll",
                        $"{typeof(WorkerInstanceManager).Assembly.GetName().Name}.dll",
                        $"{typeof(Newtonsoft.Json.JsonConvert).Assembly.GetName().Name}.dll",
                        //$"{typeof(System.Text.Json.JsonDocument).Assembly.GetName().Name}.dll",
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
                    new InitInstanceParams()
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
            var baseMessage = this.options.MessageSerializer.Deserialize<BaseMessage>(rawMessage);
            if (baseMessage.MessageType == nameof(InitInstanceComplete))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(InitInstanceComplete)}: {rawMessage}");
                this.initTask.SetResult(true);
                this.IsInitialized = true;
                return;
            }

            if (baseMessage.MessageType == nameof(InitWorkerComplete))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(InitWorkerComplete)}: {rawMessage}");
                this.initWorkerTask.SetResult(true);
                return;
            }

            if (baseMessage.MessageType == nameof(MethodCallResult))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(MethodCallResult)}: {rawMessage}");
                var message = this.options.MessageSerializer.Deserialize<MethodCallResult>(rawMessage);
                if (!this.messageRegister.TryGetValue(message.CallId, out var taskCompletionSource))
                {
                    throw new UnknownMessageException($"{nameof(MethodCallResult)}, message {nameof(MethodCallResult.CallId)} {message.CallId}");
                }

                taskCompletionSource.SetResult(message);
                return;
            }
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

            var handle = new EventHandle() { EventHandler = o => myHandler.Invoke(null, (TResult)o) };
            this.eventRegister.Add(handle.Id, handle);
            var message = new RegisterEvent() {
                EventName = eventName,
                EventHandleId = handle.Id,
                InstanceId = this.instanceId
            };
            var serializedMessage = this.options.MessageSerializer.Serialize(message);
            await this.worker.PostMessageAsync(serializedMessage);
            return handle;
        }

        private async Task<TResult> InvokeAsyncInternal<TResult>(Expression action)
        {
            // If Blazor ever gets multithreaded this would need to be locked for race conditions
            // However, when/if that happens, this entire project is obsolete
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

        public async Task PostMessageAsync(string message)
        {
            await this.worker.PostMessageAsync(message);
        }
    }
}
