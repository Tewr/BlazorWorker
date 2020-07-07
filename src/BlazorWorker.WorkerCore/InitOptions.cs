using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWorker.Core
{
    /// <summary>
    /// Options for initializing the worker.
    /// </summary>
    public class WorkerInitOptions
    {
        /// <summary>
        /// Creates a new instance of WorkerInitOptions
        /// </summary>
        public WorkerInitOptions()
        {
            DependentAssemblyFilenames = new string[] { };
        }

        /// <summary>
        /// Specifies a list of assembly files names (dlls) that should be loaded when initializing the worker.
        /// </summary>
        public string[] DependentAssemblyFilenames { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for sending messages to the worker. Experts only.
        /// </summary>
        public string MessageEndPoint { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for instanciating the worker. Experts only.
        /// </summary>
        public string InitEndPoint { get; set; }

        /// <summary>
        /// Unique blazor identifier for handling callbacks. As referenced by JSInvokableAttribute. Experts only.
        /// </summary>
        public string CallbackMethod { get; set; }

        /// <summary>
        /// If set to true, deducts the name of the assembly containing the service using the service type assembly name + dll extension as file name, and adds it as a dependency.
        /// </summary>
        public bool ConventionalServiceAssembly { get; set; }

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {

            return new WorkerInitOptions
            {
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                DependentAssemblyFilenames = this.DependentAssemblyFilenames
                    .Concat(initOptions.DependentAssemblyFilenames)
                    .Distinct()
                    .ToArray(),
                ConventionalServiceAssembly = initOptions.ConventionalServiceAssembly,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
            };
        }
    }

    /// <summary>
    /// Contains convenience extensions for <see cref="WorkerInitOptions"/>
    /// </summary>
    public static class WorkerInitOptionsExtensions
    {
        /// <summary>
        /// Deducts the name of the assembly containing the service using the  the service type assembly name + dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions UseConventionalServiceAssembly(this WorkerInitOptions source)
        {
            source.ConventionalServiceAssembly = true;
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified type using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddConventionalDependencyFor<T>(this WorkerInitOptions source)
        {
            source.DependentAssemblyFilenames =
                source.DependentAssemblyFilenames.Concat(new[] {$"{typeof(T).Assembly.GetName().Name}.dll" }).ToArray();
            return source;
        }

        /// <summary>
        /// Registers the neccessary dependencies for instanciating <see cref="System.Net.Http.HttpClient"/> in the background service.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddHttpClient(this WorkerInitOptions source)
        {
            source.DependentAssemblyFilenames = 
                source.DependentAssemblyFilenames.Concat(new[] {  
                    "System.Net.Http.dll",
                    "System.Net.Http.WebAssemblyHttpHandler.dll" }).ToArray();
            return source;
        }
    }
}