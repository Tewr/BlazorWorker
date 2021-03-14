using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    internal class WorkerBackgroundServiceProxy {
        private static long idSource;
        internal static readonly string InitEndPoint;
        public static long GetNextId() => ++idSource;
        static WorkerBackgroundServiceProxy()
        {
            var wim = typeof(WorkerInstanceManager);
            InitEndPoint = $"[{wim.Assembly.GetName().Name}]{wim.FullName}:{nameof(WorkerInstanceManager.Init)}";
        }

    }

    internal class WorkerBackgroundServiceProxy<T> : IWorkerBackgroundService<T> where T : class
    {
        private readonly IWorker worker;
        private readonly WebWorkerOptions options;
        private readonly long instanceId;

        private readonly MessageHandlerRegistry messageHandlerRegistry;
        private readonly TaskRegister initTask = new TaskRegister();
        private readonly TaskRegister disposeTaskRegistry = new TaskRegister();
        private TaskCompletionSource<bool> initWorkerTask;
        private readonly TaskRegister<MethodCallResult> messageRegister
            = new TaskRegister<MethodCallResult>();
        private readonly TaskRegister<InitInstanceFromFactoryComplete> initFromFactoryRegister
            = new TaskRegister<InitInstanceFromFactoryComplete>();
        private Dictionary<long, EventHandle> eventRegister
            = new Dictionary<long, EventHandle>();

        public bool IsInitialized { get; private set; }
        public bool IsDisposed { get; private set; }

        private bool disposing;

        /// <summary>
        /// Attached objects, notably parent worker proxy which may have been created without consumer being directly able to dispose
        /// </summary>
        public List<IAsyncDisposable> Disposables { get; } = new List<IAsyncDisposable>();

        internal WorkerBackgroundServiceProxy(
            IWorker worker,
            WebWorkerOptions options)
        {
            this.worker = worker;
            this.options = options;
            this.instanceId = WorkerBackgroundServiceProxy.GetNextId();

            this.messageHandlerRegistry = new MessageHandlerRegistry(this.options.MessageSerializer);
            this.messageHandlerRegistry.Add<InitInstanceComplete>(OnInitInstanceComplete);
            this.messageHandlerRegistry.Add<InitInstanceFromFactoryComplete>(OnInitInstanceFromFactoryComplete);
            this.messageHandlerRegistry.Add<InitWorkerComplete>(OnInitWorkerComplete);
            this.messageHandlerRegistry.Add<DisposeInstanceComplete>(OnDisposeInstanceComplete);
            this.messageHandlerRegistry.Add<EventRaised>(OnEventRaised);
            this.messageHandlerRegistry.Add<MethodCallResult>(OnMethodCallResult);
        }

        private void OnDisposeInstanceComplete(DisposeInstanceComplete message)
        {
            if (!this.disposeTaskRegistry.TryGetValue(message.CallId, out var disposeTask)) {
                return;
            }

            if (message.IsSuccess)
            {
                disposeTask.SetResult(true);
            }
            else
            {
                disposeTask.SetException(message.Exception);
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
            workerInitOptions ??= new WorkerInitOptions();

            if (this.IsInitialized)
            {
                return;
            }

            if (!this.worker.IsInitialized)
            {
                this.initWorkerTask = new TaskCompletionSource<bool>();

                if (workerInitOptions.UseConventionalServiceAssembly)
                {
                    workerInitOptions.AddAssemblyOf<T>();
                }

                await this.worker.InitAsync(new WorkerInitOptions {
                    DependentAssemblyFilenames = 
                        WorkerBackgroundServiceDependencies.DependentAssemblyFilenames,
                    InitEndPoint = WorkerBackgroundServiceProxy.InitEndPoint
                }.MergeWith(workerInitOptions));

                this.worker.IncomingMessage += OnMessage;

                await initWorkerTask.Task;
                if (this.worker is WorkerProxy proxy) { 
                    proxy.IsInitialized = true; 
                }
            }
            else
            {
                this.worker.IncomingMessage += OnMessage;
            }
            

            var (callId, initTask) = this.initTask.CreateAndAdd();

            var message = this.options.MessageSerializer.Serialize(
                    new InitInstance
                    {
                        CallId = callId,
                        WorkerId = this.worker.Identifier, // TODO: This should not really be necessary?
                        InstanceId = instanceId,
                        AssemblyName = typeof(T).Assembly.FullName,
                        TypeName = typeof(T).FullName
                    });

            if (workerInitOptions.Debug)
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}." +
                    $"{nameof(WorkerBackgroundServiceProxy<T>.InitAsync)}()" +
                    $": {this.worker.Identifier} {message}");
            }

            await this.worker.PostMessageAsync(message);
            await initTask.Task;
            this.IsInitialized = true;
        }

        public async Task<WorkerBackgroundServiceProxy<TService>> InitFromFactoryAsync<TService>(Expression<Func<T,TService>> expression) where TService:class
        {
            if (!this.IsInitialized)
            {
                throw new InvalidOperationException($"{nameof(InitFromFactoryAsync)}: Proxy must be initialized.");
            }

            if (!this.worker.IsInitialized)
            {
                throw new InvalidOperationException($"{nameof(InitFromFactoryAsync)}: Worker must be initialized.");
            }

            var (callId, initInstanceTaskSource) = this.initFromFactoryRegister.CreateAndAdd();

            var newProxy = new WorkerBackgroundServiceProxy<TService>(this.worker, this.options);
            var serializedExpression = this.options.ExpressionSerializer.Serialize(expression);

            var message = this.options.MessageSerializer.Serialize(
                    new InitInstanceFromFactory
                    {
                        WorkerId = this.worker.Identifier,
                        InstanceId = newProxy.instanceId,
                        CallId = callId,
                        FactoryInstanceId = instanceId,
                        SerializedFactoryExpression = serializedExpression
                    });

            await this.worker.PostMessageAsync(message);
            await initInstanceTaskSource.Task;
            
            newProxy.worker.IncomingMessage += newProxy.OnMessage;
            newProxy.IsInitialized = true;

            return newProxy;
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
            if (message.InstanceId != this.instanceId)
            {
                return;
            }

            if (!this.eventRegister.TryGetValue(message.EventHandleId, out var eventHandle))
            {
#if DEBUG
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(EventRaised)}: Unknown event id {message.EventHandleId}");
#endif
                return;
            }

            OnEventRaised(eventHandle, message.ResultPayload);
        }

        private void OnInitWorkerComplete(InitWorkerComplete message)
        {
            this.initWorkerTask?.SetResult(true);
        }

        private void OnInitInstanceComplete(InitInstanceComplete message)
        {
            if (!this.initTask.TryGetValue(message.CallId, out var initTask))
            {
                return;
            }

            if (message.IsSuccess)
            {
                initTask?.SetResult(true);
            }
            else
            {
                initTask?.SetException(message.Exception);
            }
        }

        private void OnInitInstanceFromFactoryComplete(InitInstanceFromFactoryComplete message)
        {
            if (!this.initFromFactoryRegister.TryGetValue(message.CallId, out var taskCompletionSource))
            {
                return;
            }

            this.initFromFactoryRegister.Remove(message.CallId);

            if (message.IsSuccess)
            {
                taskCompletionSource.SetResult(message);
            }else
            {
                taskCompletionSource.SetException(message.Exception);
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

        public async Task RunAsync<TResult>(Expression<Func<T, Task>> action)
        {
            await InvokeAsyncInternal<object>(action, new InvokeOptions { AwaitResult = true });
        }

        public async Task<TResult> RunAsync<TResult>(Expression<Func<T, Task<TResult>>> function)
        {
            return await InvokeAsyncInternal<TResult>(function, new InvokeOptions { AwaitResult = true });
        }

        public async Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler)
        {
            var typeExposingEvent = typeof(T);
            var eventSignature = typeExposingEvent.GetEvent(eventName ?? throw new ArgumentNullException(nameof(eventName)));
            if (eventSignature == null)
            {
                throw new ArgumentException($"Type '{typeExposingEvent.FullName}' does not expose any event named '{eventName}'");
            }

            if (!eventSignature.EventHandlerType.IsAssignableFrom(typeof(EventHandler<TResult>)))
            {
                throw new ArgumentException($"Event '{typeExposingEvent.FullName}.{eventName}' can only be assigned an event listener of type '{eventSignature.EventHandlerType}'");
            }

            var handle = new EventHandle
            {
                EventHandler =
                payload => {
                    var typedPayload = this.options.MessageSerializer.Deserialize<TResult>(payload);
                    myHandler.Invoke(null, typedPayload);
                }
            };

            this.eventRegister.Add(handle.Id, handle);
            var message = new RegisterEvent
            {
                EventName = eventName,
                EventHandlerTypeArg = typeof(TResult).AssemblyQualifiedName,
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

        private Task<TResult> InvokeAsyncInternal<TResult>(Expression action)
        {
            return InvokeAsyncInternal<TResult>(action, InvokeOptions.Default);
        }

        private async Task<TResult> InvokeAsyncInternal<TResult>(Expression action, InvokeOptions invokeOptions)
        {
            // If Blazor ever gets multithreaded this would need to be locked for race conditions
            // However, when/if that happens, most of this project is obsolete anyway
            var (id, taskCompletionSource) = this.messageRegister.CreateAndAdd();

            var expression = this.options.ExpressionSerializer.Serialize(action);
            var methodCallParams = new MethodCallParams
            {
                AwaitResult = invokeOptions.AwaitResult,
                WorkerId = this.worker.Identifier,
                InstanceId = instanceId,
                SerializedExpression = expression,
                CallId = id
            };

            var methodCall = this.options.MessageSerializer.Serialize(methodCallParams);

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

        public async ValueTask DisposeAsync()
        {
            if (this.IsDisposed || disposing)
            {
                return;
            }

            disposing = true;
            try
            {
                var (callId, disposeTask) = this.disposeTaskRegistry.CreateAndAdd();
                var message = this.options.MessageSerializer.Serialize(
                       new DisposeInstance
                       {
                           CallId = callId,
                           InstanceId = instanceId
                       });

                await this.worker.PostMessageAsync(message);

                await disposeTask.Task;

                // This is neccessary as the worker may continue to live
                worker.IncomingMessage -= OnMessage;
            
                foreach (var item in this.Disposables)
                {
                    await item.DisposeAsync();
                }

                this.IsDisposed = true;
            }
            finally
            {
                disposing = false;
            }
        }

        private class InvokeOptions
        {
            public static readonly InvokeOptions Default = new InvokeOptions();

            public bool AwaitResult { get; set; }
        }
    }
}
