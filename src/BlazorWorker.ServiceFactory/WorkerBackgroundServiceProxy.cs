using Microsoft.JSInterop;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Serialize.Linq;
using BlazorWorker.BackgroundServiceFactory.Shared;
using Serialize.Linq.Extensions;
using BlazorWorker.Core;
using System.Collections.Generic;
using MonoWorker.BackgroundServiceHost;

namespace BlazorWorker.BackgroundServiceFactory
{
    public class WorkerBackgroundServiceProxy<T> : IWorkerBackgroundService<T> where T : class
    {
        private readonly IWorker worker;
        private readonly WebWorkerOptions options;
        private static readonly string initEndPoint;
        private static long idSource;
        private long instanceId;
        private TaskCompletionSource<bool> initTask;
        // This doesnt really need to be static but easier to debug if messages have application-wide unique ids
        private static long messageRegisterIdSource;
        private Dictionary<ulong, TaskCompletionSource<MethodCallResult>> messageRegister
            = new Dictionary<ulong, TaskCompletionSource<MethodCallResult>>();

        public bool IsInitialized { get; private set; }

        static WorkerBackgroundServiceProxy()
        {
            var workerInitMethod = typeof(WorkerInstanceManager);
            initEndPoint = $"[{workerInitMethod.Assembly.GetName().Name}]{workerInitMethod.FullName}:{nameof(WorkerInstanceManager.Init)}";
        }

        public WorkerBackgroundServiceProxy(
            IWorker worker,
            WebWorkerOptions options)
        {
            this.worker = worker;
            this.options = options;
            this.instanceId = ++idSource;
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
                await this.worker.InitAsync(new WorkerInitOptions() { 
                    staticAssemblyRefs = new[] { $"{this.GetType().Assembly.GetName().Name}.dll" },
                    initEndPoint = initEndPoint
                }.MergeWith(workerInitOptions));

                this.worker.IncomingMessage += OnMessage;
            }

            var message = this.options.Serializer.Serialize(
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
            var baseMessage = this.options.Serializer.Deserialize<BaseMessage>(rawMessage);
            if (baseMessage.MessageType == nameof(InitInstanceComplete))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(InitInstanceComplete)}: {rawMessage}");
                this.initTask.SetResult(true);
                this.IsInitialized = true;
                return;
            }

            if (baseMessage.MessageType == nameof(MethodCallResult))
            {
                Console.WriteLine($"{nameof(WorkerBackgroundServiceProxy<T>)}: {nameof(MethodCallResult)}: {rawMessage}");
                var message = this.options.Serializer.Deserialize<MethodCallResult>(rawMessage);
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

        private async Task<TResult> InvokeAsyncInternal<TResult>(Expression action)
        {
            var id = ++messageRegisterIdSource;
            var taskCompletionSource = new TaskCompletionSource<MethodCallResult>();

            var methodCall = GetCall(action, id);

            await this.worker.PostMessageAsync(methodCall);

            var returnMessage = await taskCompletionSource.Task;
            if (returnMessage.IsException)
            {
                throw returnMessage.Exception;
            }
            if (string.IsNullOrEmpty(returnMessage.ResultPayload))
            {
                return default;
            }

            return this.options.Serializer.Deserialize<TResult>(returnMessage.ResultPayload);
        }

        private string GetCall(Expression action, long id)
        {
            var args = action.ToExpressionNode();

            var methodCall = new MethodCallParams
            {
                WorkerId = this.worker.Identifier,
                InstanceId = instanceId,
                MethodCall = args,
                CallId = id
            };

            return (this.options.Serializer ?? DefaultSerializer.Instance).Serialize(methodCall);
        }
    }
}
