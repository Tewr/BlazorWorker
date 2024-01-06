using BlazorWorker.WorkerCore;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.Extensions.JSRuntime
{
    public partial class BlazorWorkerJSRuntime : IJSRuntime, IJSInProcessRuntime
    {
        private static bool isJsInitialized;

        public IBlazorWorkerJSRuntimeSerializer Serializer { get; set; }

        public BlazorWorkerJSRuntime()
        {
            this.Serializer = new DefaultBlazorWorkerJSRuntimeSerializer(this);
        }

        public T Invoke<T>(string identifier, params object[] args)
        {
            var resultString  = JSInvokeService.WorkerInvoke<T>(identifier, Serialize(args));

            return this.Serializer.Deserialize<T>(resultString);
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
            string resultObj;
            
            try
            {
                await EnsureInitialized();
                
                resultObj = await JSInvokeService.WorkerInvokeAsync<TValue>(identifier, serializedArgs);
            }
            catch (System.AggregateException e) when (e.InnerException is JSInvokeException)
            {
                throw e.InnerException;
            }
            cancellationToken.ThrowIfCancellationRequested();

            var result = Deserialize<TValue>(resultObj);
            return result;
        }

        private static async Task EnsureInitialized()
        {
            if (!isJsInitialized && !JSInvokeService.IsObjectDefined("BlazorWorkerJSRuntimeSerializer"))
            {
                await JSInvokeService.ImportLocalScripts("_content/Tewr.BlazorWorker.Extensions.JSRuntime/BlazorWorkerJSRuntime.js");
                isJsInitialized = true;
            }
        }

        [JSExport]
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
