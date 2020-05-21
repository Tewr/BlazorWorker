using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class Runtime
    {
        private delegate object GetGlobalObjectDelegate(string globalObjectName);

        private static GetGlobalObjectDelegate _getGlobalObjectMethod = 
                WebAssemblyBindingsLoader
                .LoadAssembly()
                .GetType($"WebAssembly.{nameof(Runtime)}")
                .GetMethod(nameof(GetGlobalObject))
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod(globalObjectName);
    }
}