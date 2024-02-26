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
#pragma warning disable 
            DependentAssemblyFilenames = Array.Empty<string>();
#pragma warning restore

#if NETSTANDARD21
            DeployPrefix = "_framework/_bin";
            WasmRoot = "_framework/wasm";
#endif
#if NET5_0_OR_GREATER
            DeployPrefix = "_framework";
            WasmRoot = "_framework";
#endif
#if DEBUG
            Debug = true;
#endif
            RuntimePreprocessorSymbols = new();
#if NET7_0
            RuntimePreprocessorSymbols.Add("NET7_0", true);
#endif
#if NET7_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET7_0_OR_GREATER", true);
#endif
#if NET8_0
            RuntimePreprocessorSymbols.Add("NET8_0", true);
#endif
#if NET8_0_OR_GREATER
            RuntimePreprocessorSymbols.Add("NET8_0_OR_GREATER", true);
#endif

        }

        /// <summary>
        /// Specifies the location of binaries
        /// </summary>
        public string DeployPrefix { get; }

        /// <summary>
        /// Specifies the location of the wasm binary
        /// </summary>
        public string WasmRoot { get; }

        /// <summary>
        /// Outputs additional debug information to the console
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Specifies a list of assembly files names (dlls) that should be loaded when initializing the worker.
        /// </summary>
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
        public string[] DependentAssemblyFilenames { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for sending messages to the worker. Experts only.
        /// </summary>
        public MethodIdentifier MessageEndPoint { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for instantiating the worker. Experts only.
        /// </summary>
        public MethodIdentifier InitEndPoint { get; set; }

        /// <summary>
        /// Unique blazor identifier for handling callbacks. As referenced by JSInvokableAttribute. Experts only.
        /// </summary>
        public string CallbackMethod { get; set; }

        /// <summary>
        /// If set to true, deducts the name of the assembly containing the service using the service type assembly name + dll extension as file name, and adds it as a dependency.
        /// </summary>
        public bool UseConventionalServiceAssembly { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for doing callbacks on self invocations from the worker. Experts only.
        /// </summary>
        public string EndInvokeCallBackEndpoint { get; set; }

        /// <summary>
        /// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives
        /// </summary>
        public Dictionary<string, bool> RuntimePreprocessorSymbols { get; set; }

        /// <summary>
        /// Sets environment variables in the target worker. 
        /// </summary>
        /// <remarks>
        /// Defaults to a single entry: DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '1'.
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public Dictionary<string, string> EnvMap { get; set; } 
            = new Dictionary<string, string>() { 
                { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" },
            };

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {
            var newEnvMap = new Dictionary<string , string>(this.EnvMap);
            if (initOptions.EnvMap != null)
            {
                foreach (var entry in initOptions.EnvMap)
                {
                    newEnvMap[entry.Key] = entry.Value;
                }
            }
            return new WorkerInitOptions
            {
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                UseConventionalServiceAssembly = initOptions.UseConventionalServiceAssembly,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
                EndInvokeCallBackEndpoint = initOptions.EndInvokeCallBackEndpoint ?? this.EndInvokeCallBackEndpoint,
                EnvMap = newEnvMap
            };
        }
    }

    /// <summary>
    /// Contains convenience extensions for <see cref="WorkerInitOptions"/>
    /// </summary>
    public static class WorkerInitOptionsExtensions
    {

        /// <summary>
        /// Adds the specified assembly filenames as dependencies
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dependentAssemblyFilenames"></param>
        /// <returns></returns>
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
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
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
        public static WorkerInitOptions AddConventionalAssemblyOfService(this WorkerInitOptions source)
        {
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified <typeparamref name="T"/> using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
        public static WorkerInitOptions AddAssemblyOf<T>(this WorkerInitOptions source)
        {
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified <paramref name="type"/> using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
        public static WorkerInitOptions AddAssemblyOfType(this WorkerInitOptions source, Type type)
        {
            return source;
        }

        /// <summary>
        /// Registers the necessary dependencies for injecting or instantiating <see cref="System.Net.Http.HttpClient"/> in the background service.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks>When this method has been called, <see cref="System.Net.Http.HttpClient"/> can be used inside the service either by instanciating it or by injection into the constructor.</remarks>
        [Obsolete("Manual dependency optimization is silently ignored in this version of BlazorWorker.")]
        public static WorkerInitOptions AddHttpClient(this WorkerInitOptions source)
        {
            return source;
        }

        /// <summary>
        /// Set the specified <paramref name="environmentVariableName"/> to the specified <paramref name="value"/> when the worker runtime has been initialized
        /// </summary>
        /// <param name="source"></param>
        /// <param name="environmentVariableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public static WorkerInitOptions SetEnv(this WorkerInitOptions source, string environmentVariableName, string value)
        {
            source.EnvMap[environmentVariableName] = value;
            return source;
        }
    }

    public class MethodIdentifier
    {
        public string AssemblyName { get; set; }
        public string FullMethodName { get; set; }
    }
}