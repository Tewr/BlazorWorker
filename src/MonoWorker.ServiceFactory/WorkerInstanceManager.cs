using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.BackgroundServiceFactory.Shared;
using MonoWorker.Core;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MonoWorker.BackgroundServiceHost
{
    public class WorkerInstanceManager : IWorkerInstance
    {
        public readonly Dictionary<long, object> instances = new Dictionary<long, object>();

        public static readonly WorkerInstanceManager Instance = new WorkerInstanceManager();
        private readonly ISerializer serializer;

        public WorkerInstanceManager()
        {
            this.serializer = new DefaultSerializer();
        }

        public static void Init() {
            MessageService.Message += Instance.OnMessage;
        }

        public void PostMessage(string message)
        {
            MessageService.PostMessage(message);
        }

        private void OnMessage(object sender, string message)
        {
            // TODO: deserialize messagre
            var baseMessage = this.serializer.Deserialize<BaseMessage>(message);
            if (baseMessage.MessageType == nameof(InitInstanceParams))
            {
                var initMessage = this.serializer.Deserialize<InitInstanceParams>(message);

            }
        }

        public void InitInstance(InitInstanceParams createInstanceInfo)
        {
            var assembly = Assembly.LoadFrom(createInstanceInfo.AssemblyName);
            if (assembly == null)
            {
                throw new InitWorkerInstanceException($"Unable to to load assembly {createInstanceInfo.AssemblyName}");
            }
            var type = assembly.GetType(createInstanceInfo.TypeName);

            if (assembly == null)
            {
                throw new InitWorkerInstanceException($"Unable to to load type {createInstanceInfo.TypeName} from {assembly.FullName}");
            }

            instances[createInstanceInfo.InstanceId] = Activator.CreateInstance(type);
        }

        internal object Call(MethodCallParams instanceMethodCallParams)
        {
            var instance = instances[instanceMethodCallParams.InstanceId];
            var lambda = instanceMethodCallParams.MethodCall.ToExpression() as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            var methodInfo = dynamicDelegate.GetMethodInfo();
            return methodInfo.Invoke(instance, new object[] { });
        }
    }
}
