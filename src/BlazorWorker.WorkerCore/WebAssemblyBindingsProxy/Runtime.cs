using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class Runtime
    {
        private delegate object GetGlobalObjectDelegate(string globalObjectName);

        private static GetGlobalObjectDelegate _getGlobalObjectMethod = 
                AssemblyProvider.ResourceAssembly
                .GetType($"WebAssembly.{nameof(Runtime)}")
                .GetMethod(nameof(GetGlobalObject), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string) }, null)
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod(globalObjectName);
    }
}