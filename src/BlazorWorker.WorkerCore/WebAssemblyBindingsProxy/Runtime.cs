using System;
using System.Linq;
using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    /// <summary>
    /// Invocation for Runtime class in https://github.com/mono/mono/blob/1bdb6cc55c3f5813407be9eb9bfe05aa891e1faf/sdks/wasm/src/driver.c#L353
    /// </summary>
    internal class Runtime
    {
#if NETSTANDARD21
        private const string assembly = "WebAssembly.Bindings";
        private static readonly string type = $"WebAssembly.{nameof(Runtime)}";
#endif

#if NET5 || NET6
        private const string assembly = "System.Private.Runtime.InteropServices.JavaScript";
        private static readonly string type = $"System.Runtime.InteropServices.JavaScript.{nameof(Runtime)}";
#endif
        private delegate object GetGlobalObjectDelegate(string globalObjectName);
        private delegate object TypedArrayCopyFromDelegate(int jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult);


        private static Assembly SourceAssembly => Assembly.Load(assembly) 
            ?? throw new InvalidOperationException($"Unable to load assembly {assembly}");

        private static GetGlobalObjectDelegate _getGlobalObjectMethod =
                SourceAssembly
                .GetType(type)?
                .GetMethod(nameof(GetGlobalObject))?
                .CreateDelegate(typeof(GetGlobalObjectDelegate)) as GetGlobalObjectDelegate;

        private static TypedArrayCopyFromDelegate _typedArrayCopyFrom =
            SourceAssembly
            .GetType("Interop+Runtime")?
            .GetRuntimeMethods().FirstOrDefault(m => m.Name == nameof(TypedArrayCopyFrom))?
            .CreateDelegate(typeof(TypedArrayCopyFromDelegate)) as TypedArrayCopyFromDelegate;

        public static object GetGlobalObject(string globalObjectName) => _getGlobalObjectMethod?.Invoke(globalObjectName) 
            ?? throw new InvalidOperationException($"Unable to load method {type}.{nameof(GetGlobalObject)} from assembly {assembly}");

        public static object TypedArrayCopyFrom(int jsHandle, int arrayPtr, int begin, int end, int bytesPerElement, out int exceptionalResult)
        {
            exceptionalResult = 0;
            if (_typedArrayCopyFrom == null)
                Console.WriteLine("TypedArrayCopyFrom: _typedArrayCopyFrom == null");
            return _typedArrayCopyFrom?.Invoke(jsHandle, arrayPtr, begin, end, bytesPerElement, out exceptionalResult); 
        }
    }

}