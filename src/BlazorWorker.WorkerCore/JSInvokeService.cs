using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    public class JSInvokeService
    {
        private static readonly DOMObject self = DOMObject.Self;

        private static readonly TaskRegister selfInvokeRegister = new TaskRegister();

        public static T Invoke<T>(string method, params object[] args)
        {
            return (T)self.Invoke(method, args);
        }

        public static Task InvokeVoidAsync(string method, string serializedArgs)
        {
            return PrivateInvokeAsync(method, CancellationToken.None, serializedArgs, true);
        }

        public static Task<object> InvokeAsync(
            string method,
            CancellationToken cancellationToken,
            string serializedArgs,
            string serializer = "nativejson")
        {
            return PrivateInvokeAsync(method, cancellationToken, serializedArgs, false, serializer);
        }

       

        public static Task<object> InvokeAsync(string method, string serializedArgs,
            string serializer = "nativejson")
        {
            return InvokeAsync(method, CancellationToken.None, serializedArgs, serializer);
        }

        public static void EndInvokeCallBack(long id, bool isError, object result)
        {
            
            if (!selfInvokeRegister.TryRemove(id, out var taskCompletionSource))
            {
#if DEBUG
                Console.WriteLine($"{nameof(JSInvokeService)}.{nameof(EndInvokeCallBack)}: unknown task id {id}");
#endif
                return;
            }

            //Console.WriteLine($"{nameof(JSInvokeService)}.{nameof(EndInvokeCallBack)}({id}): result was {(result?.ToString() ?? "(null)") }");

            // TODO: Error management
            if (!isError)
            {
                taskCompletionSource.SetResult(result);
            }
            else
            {
                taskCompletionSource.SetException(new JSInvokeException(result.ToString()));
            }
        }

        /// <summary>
        /// Prepending the specified <paramref name="relativeUrls"/> with the base path of the application, invokes the importScripts() method of the WorkerGlobalScope interface, which synchronously imports one or more scripts into the worker's scope.
        /// </summary>
        /// <param name="relativeUrls"></param>
        public static void ImportLocalScripts(params string[] relativeUrls)
        {
            Invoke<object>("importLocalScripts", relativeUrls);
        }

        /// <summary>
        /// Returns <c>true</c> if the specified <paramref name="objectPath"/> is defined on the global scope
        /// </summary>
        /// <param name="objectPath"></param>
        public static bool IsObjectDefined(string objectPath)
        {
            return Invoke<bool>("isObjectDefined", objectPath);
        }

        private static async Task<object> PrivateInvokeAsync(
           string method,
           CancellationToken cancellationToken,
           string serializedArgs,
           bool isVoid,
           string serializer = "nativejson")
        {
            var (invokeId, task) = selfInvokeRegister.CreateAndAdd();
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() => {
                    task.SetCanceled();
                    selfInvokeRegister.TryRemove(invokeId, out var _);
                });
            }
            //Console.WriteLine($"{nameof(JSInvokeService)}.{nameof(PrivateInvokeAsync)}: {method}({serializedArgs}) isVoid: {isVoid} ");
            self.Invoke("beginInvokeAsync", serializer, invokeId.ToString(), method, isVoid, serializedArgs);
            return await task.Task;
        }
    }
}
