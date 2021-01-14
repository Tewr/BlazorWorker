using BlazorWorker.Core;
using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    internal class WorkerBackgroundServiceProxy<T> : IWorkerBackgroundService<T>, IWorkerBackgroundServiceFactory<T> where T : class
    {
        private readonly IWorker worker;
        private readonly WebWorkerOptions options;
        private static readonly string InitEndPoint;
        private static long idSource;
        private readonly long instanceId;
        private readonly ISerializer messageSerializer;
        private readonly object expressionSerializer;
        private MessageHandlerRegistry messageHandlerRegistry;
        private TaskCompletionSource<bool> initTask;
        private TaskCompletionSource<bool> disposeTask;
        private TaskCompletionSource<bool> initWorkerTask;
        // This doesnt really need to be static but easier to debug if messages have application-wide unique ids
        private static long messageRegisterIdSource;
        private readonly Dictionary<long, TaskCompletionSource<MethodCallResult>> messageRegister
            = new Dictionary<long, TaskCompletionSource<MethodCallResult>>();
        private static long initFromFactoryIdSource;
        private readonly Dictionary<long, TaskCompletionSource<InitInstanceFromFactoryComplete>> initFromFactoryRegister
            = new Dictionary<long, TaskCompletionSource<InitInstanceFromFactoryComplete>>();

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
            this.messageHandlerRegistry.Add<InitInstanceFromFactoryComplete>(OnInitInstanceFromFactoryComplete);
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
            workerInitOptions ??= new WorkerInitOptions();
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

                if (workerInitOptions.UseConventionalServiceAssembly)
                {
                    workerInitOptions.AddAssemblyOf<T>();
                }

                await this.worker.InitAsync(new WorkerInitOptions {
                    DependentAssemblyFilenames = 
                        WorkerBackgroundServiceDependencies.DependentAssemblyFilenames,
                    InitEndPoint = InitEndPoint
                }.MergeWith(workerInitOptions));

                this.worker.IncomingMessage += OnMessage;
                await initWorkerTask.Task;
            }

            var message = this.options.MessageSerializer.Serialize(
                    new InitInstance
                    {
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

        private void OnInitInstanceFromFactoryComplete(InitInstanceFromFactoryComplete message)
        {
            if (!this.initFromFactoryRegister.TryGetValue(message.CallId, out var taskCompletionSource))
            {
                return;
            }

            taskCompletionSource.SetResult(message);
            this.initFromFactoryRegister.Remove(message.CallId);
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

        public async Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler) =>
            await RegisterEventListenerAsync(eventName, myHandler, null);
        

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

        public async Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler, Expression instanceExpression)
        {
            Type typeExposingEvent;
            if (instanceExpression is null)
            {
                typeExposingEvent = typeof(T);
            }
            else
            {
                typeExposingEvent = (instanceExpression as LambdaExpression)?.ReturnType;
            }
            
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
                    var typedPayload = this.messageSerializer.Deserialize<TResult>(payload);
                    myHandler.Invoke(null, typedPayload);
                }
            };

            string serializedInstanceExpression = null;
            if (instanceExpression != null)
            {
                serializedInstanceExpression = 
                    this.options.ExpressionSerializer.Serialize(instanceExpression);
            }

            this.eventRegister.Add(handle.Id, handle);
            var message = new RegisterEvent()
            {
                EventName = eventName,
                InstanceExpression = serializedInstanceExpression,
                EventHandlerTypeArg = typeof(TResult).FullName,
                EventHandleId = handle.Id,
                InstanceId = this.instanceId
            };

            var serializedMessage = this.options.MessageSerializer.Serialize(message);

            await this.worker.PostMessageAsync(serializedMessage);

            return handle;
        }


        private Task<TResult> InvokeAsyncInternal<TResult>(Expression action)
        {
            return InvokeAsyncInternal<TResult>(action, InvokeOptions.Default);
        }

        private async Task<TResult> InvokeAsyncInternal<TResult>(Expression action, InvokeOptions invokeOptions)
        {
            // If Blazor ever gets multithreaded this would need to be locked for race conditions
            // However, when/if that happens, most of this project is obsolete anyway
            var id = ++messageRegisterIdSource;
            var taskCompletionSource = new TaskCompletionSource<MethodCallResult>();
            this.messageRegister.Add(id, taskCompletionSource);

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

        public async Task<TResult> InitFromFactory<TResult>(Expression factoryExpression)
        {
            // If Blazor ever gets multithreaded this would need to be locked for race conditions
            // However, when/if that happens, most of this project is obsolete anyway
            var id = ++initFromFactoryIdSource;
            var taskCompletionSource = new TaskCompletionSource<InitInstanceFromFactoryComplete>();
            this.initFromFactoryRegister.Add(id, taskCompletionSource);

            var expression = this.options.ExpressionSerializer.Serialize(factoryExpression);
            var methodCallParams = new InitInstanceFromFactory
            {
                WorkerId = this.worker.Identifier,
                FactoryInstanceId = instanceId,
                InstanceId = ++idSource,
                CallId = id
            };

            var methodCall = this.options.MessageSerializer.Serialize(methodCallParams);

            await this.worker.PostMessageAsync(methodCall);

            var returnMessage = await taskCompletionSource.Task;
            if (returnMessage.IsException)
            {
                throw new AggregateException($"Worker exception: {returnMessage.Exception.Message}", returnMessage.Exception);
            }
            if (string.IsNullOrEmpty(returnMessage.ResultPayload))
            {
                return default;
            }

            return this.options.MessageSerializer.Deserialize<TResult>(returnMessage.ResultPayload);
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

        private class InvokeOptions
        {
            public static readonly InvokeOptions Default = new InvokeOptions();

            public bool AwaitResult { get; set; }
        }
    }
}
