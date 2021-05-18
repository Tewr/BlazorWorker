using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {
        private static bool isJsInitialized;

        public IBlazorWorkerJSRuntimeSerializer Serializer { get; set; }

        public BlazorWorkerJSRuntime()
        {
            this.Serializer = new DefaultBlazorWorkerJSRuntimeSerializer(this);
        }

        public T Invoke<T>(string identifier, params object[] args)
        {
            return JSInvokeService.Invoke<T>(identifier, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args ?? new object[] { });
        }

        private string Serialize(object obj)
        {
            return Serializer.Serialize(obj);
        }

        private T Deserialize<T>(string serializedObject)
        {
            if (serializedObject is null)
            {
                return default;
            }

            return Serializer.Deserialize<T>(serializedObject);
        }

        public async ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, 
            CancellationToken cancellationToken, 
            object[] args)
        {
            var serializedArgs = Serialize(args);
            object resultObj;
            
            try
            {
                EnsureInitialized();

                resultObj = await JSInvokeService.InvokeAsync(identifier,
                    cancellationToken, serializedArgs, "BlazorWorkerJSRuntimeSerializer");
            }
            catch (System.AggregateException e) when (e.InnerException is JSInvokeException)
            {
                throw e.InnerException;
            }


            //Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeAsync)}({identifier}): deserializing result: {resultObj?.ToString()} ");
            var result = Deserialize<TValue>(resultObj as string);
            //Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeAsync)}({identifier}): returning deserialized result of type {(result?.GetType().ToString() ?? "(null)")}: {result?.ToString()} ");
            return result;
        }

        private static void EnsureInitialized()
        {
            if (!isJsInitialized && !JSInvokeService.IsObjectDefined("BlazorWorkerJSRuntimeSerializer"))
            {
                JSInvokeService.ImportLocalScripts("_content/BlazorWorker.Extensions.JSRuntime/BlazorWorkerJSRuntime.js");
                isJsInitialized = true;
            }
        }

        public static string InvokeMethod(string objectInstanceId, string argsString)
        {
#if DEBUG
            Console.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString})");
#endif
            try
            {
                var obj = DotNetObjectReferenceTracker.GetObjectReference(long.Parse(objectInstanceId));
                var serializer = DotNetObjectReferenceTracker.GetCallbackJSRuntime(obj).Serializer;
                var callBackArgs = serializer.Deserialize<CallBackArgs>(argsString);
                var method = obj.GetType().GetMethod(
                    callBackArgs.Method, 
                    callBackArgs.MethodArgs.Select(arg => arg.GetType()).ToArray());

                var resultObj = method.Invoke(obj, callBackArgs.MethodArgs);
                if (resultObj is null)
                {
                    return null;
                }

                return serializer.Serialize(resultObj);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"{nameof(BlazorWorkerJSRuntime)}.{nameof(InvokeMethod)}({objectInstanceId}, {argsString}) error: {e.ToString()}");
                throw;
            }
        }

        public class CallBackArgs { 
            public string Method { get; set; }
            public object[] MethodArgs { get; set; }
        }
    }
}
