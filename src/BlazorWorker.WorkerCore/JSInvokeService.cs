using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorWorker.WorkerCore
{
    
    public partial class JSInvokeService
    {
        /// <summary>
        /// Prepending the specified <paramref name="relativeUrls"/> with the base path of the application, invokes the importScripts() method of the WorkerGlobalScope interface, which synchronously imports one or more scripts into the worker's scope.
        /// </summary>
        /// <param name="relativeUrls"></param>
        [JSImport("importLocalScripts")]
        public static partial void ImportLocalScripts(string[] relativeUrls);

    }
}
