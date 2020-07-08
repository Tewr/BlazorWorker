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
        public bool UseConventionalServiceAssembly { get; set; }

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {

            return new WorkerInitOptions
            {
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                DependentAssemblyFilenames = this.DependentAssemblyFilenames
                    .Concat(initOptions.DependentAssemblyFilenames)
                    .Distinct()
                    .ToArray(),
                UseConventionalServiceAssembly = initOptions.UseConventionalServiceAssembly,
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
        /// Adds the specified assembly filesnames as dependencies
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dependentAssemblyFilenames"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddAssemblies(this WorkerInitOptions source, params string[] dependentAssemblyFilenames)
        {
            source.DependentAssemblyFilenames =
                source.DependentAssemblyFilenames.Concat(dependentAssemblyFilenames).ToArray();
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the service using the the service type assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddConventionalAssemblyOfService(this WorkerInitOptions source)
        {
            source.UseConventionalServiceAssembly = true;
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified <typeparamref name="T"/> using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddAssemblyOf<T>(this WorkerInitOptions source)
        {
            source.AddAssemblies($"{typeof(T).Assembly.GetName().Name}.dll");
            return source;
        }

        /// <summary>
        /// Registers the neccessary dependencies for injecting or instanciating <see cref="System.Net.Http.HttpClient"/> in the background service.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks>When this method has been called, <see cref="System.Net.Http.HttpClient"/> can be used inside the service either by instanciating it or by injection into the constructor.</remarks>
        public static WorkerInitOptions AddHttpClient(this WorkerInitOptions source)
        {
            source.AddAssemblies("System.Net.Http.dll", "System.Net.Http.WebAssemblyHttpHandler.dll");
            return source;
        }
    }
}