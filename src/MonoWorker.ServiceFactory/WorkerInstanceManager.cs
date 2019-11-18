using BlazorWorker.BackgroundServiceFactory.Shared;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace MonoWorker.BackgroundServiceHost
{
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
