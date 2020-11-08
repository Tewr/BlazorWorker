using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class Runtime
    {
        private delegate object GetGlobalObjectDelegate(string globalObjectName);

        private static GetGlobalObjectDelegate _getGlobalObjectMethod =
                Assembly
                .Load("System.Private.Runtime.InteropServices.JavaScript")
                .GetType($"System.Runtime.InteropServices.JavaScript.{nameof(Runtime)}")
                .GetMethod(nameof(GetGlobalObject))
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod(globalObjectName);
    }
}