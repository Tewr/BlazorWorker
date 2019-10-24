using BlazorWorker.Shared;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using WebAssembly;

namespace BlazorWorker.Worker
{
    /// <summary>
    /// Entry class
    /// </summary>
    public class Worker
    {
        public static readonly WorkerInstanceManager Instance = new WorkerInstanceManager();
        private static readonly DOMObject self = new DOMObject("self");

        // todo: this string could/(should?) be a byte
        public static void MessageHandler(string message)
        {
            Console.WriteLine($"Worker.MessageHandler:{message}");
            SendMessage($"Worker.MessageHandler: ECHO {message}");
        }

        public static void SendMessage(string message)
        {
            self.Invoke("postMessage", message);
        }

        public static void Dispose()
        {
            self.Dispose();
        }

        public static void InitInstance(InitInstanceParams createInstanceInfo) 
            => Instance.InitInstance(createInstanceInfo);

        public static object Call(InstanceMethodCallParams instanceMethodCallParams)
           => Instance.Call(instanceMethodCallParams);
    }

    public class WorkerInstanceManager : IWorkerInstance
    {
        public readonly Dictionary<string, object> instances = new Dictionary<string, object>();

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

        internal object Call(InstanceMethodCallParams instanceMethodCallParams)
        {
            var instance = instances[instanceMethodCallParams.InstanceId];
            var lambda = instanceMethodCallParams.MethodCall.ToExpression() as LambdaExpression;
            var dynamicDelegate = lambda.Compile();
            var methodInfo = dynamicDelegate.GetMethodInfo();
            return methodInfo.Invoke(instance, new object[] { });
        }
    }
}
