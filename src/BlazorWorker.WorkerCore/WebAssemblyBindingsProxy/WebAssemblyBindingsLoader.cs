using System;
using System.IO;
using System.Reflection;

namespace BlazorWorker.WorkerCore.WebAssemblyBindingsProxy
{
    internal class WebAssemblyBindingsLoader
    {
        const string ResourceName = "BlazorWorker.WorkerCore.WebAssemblyBindingsProxy.WebAssembly.Bindings.dll";
        public static Assembly LoadAssembly()
        {
            var thisAssembly = typeof(WebAssemblyBindingsLoader).Assembly;
            var rsStream = thisAssembly.GetManifestResourceStream(ResourceName);

            if (rsStream == null)
            {
                throw new InvalidOperationException($"Unable to load resource '{ResourceName}'");
            }

            using (var ms = new MemoryStream())
            using (rsStream)
            {
                rsStream.CopyTo(ms);
                return Assembly.Load(ms.ToArray());
            }
        }
    }
}