using System;
using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class Runtime
    {
#if NETSTANDARD21
        private const string assembly = "Webassembly.Bindings";
        private static readonly string type = $"Webassembly.{nameof(Runtime)}";
#endif

#if NET5
        private const string assembly = "System.Private.Runtime.InteropServices.JavaScript";
        private static readonly string type = $"System.Runtime.InteropServices.JavaScript.{nameof(Runtime)}";
#endif
        private delegate object GetGlobalObjectDelegate(string globalObjectName);

        private static GetGlobalObjectDelegate _getGlobalObjectMethod =
                Assembly
                .Load(assembly)?
                .GetType(type)?
                .GetMethod(nameof(GetGlobalObject))?
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod?.Invoke(globalObjectName) 
            ?? throw new InvalidOperationException($"Unable to laod type {type} from assembly {assembly}");
    }
}