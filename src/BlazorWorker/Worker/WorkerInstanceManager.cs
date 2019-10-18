using BlazorWorker.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BlazorWorker.Worker
{
    /// <summary>
    /// Entry class
    /// </summary>
    public class Worker
    {
        public static readonly WorkerInstanceManager WorkerInstance = new WorkerInstanceManager();
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
    }
}
