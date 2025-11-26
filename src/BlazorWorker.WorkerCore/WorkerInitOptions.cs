using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlazorWorker.Core
{
    /// <summary>
    /// Options for initializing the worker.
    /// </summary>
    public class WorkerInitOptions
    {
        /// <summary>
        /// Default Runtime-version specific preprocessor symbols that are available at runtime.
        /// </summary>
        /// <remarks>https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives</remarks>
        public static IReadOnlyDictionary<string, bool> DefaultRuntimePreprocessorSymbols 
            => _staticRuntimePreprocessorSymbols;

        private static readonly Dictionary<string, bool> _staticRuntimePreprocessorSymbols;

        static WorkerInitOptions()
        {
            _staticRuntimePreprocessorSymbols = new();
#if NET7_0
            _staticRuntimePreprocessorSymbols.Add("NET7_0", true);
#endif
#if NET7_0_OR_GREATER
            _staticRuntimePreprocessorSymbols.Add("NET7_0_OR_GREATER", true);
#endif
#if NET8_0
            _staticRuntimePreprocessorSymbols.Add("NET8_0", true);
#endif
#if NET8_0_OR_GREATER
            _staticRuntimePreprocessorSymbols.Add("NET8_0_OR_GREATER", true);
#endif
#if NET9_0
            _staticRuntimePreprocessorSymbols.Add("NET9_0", true);
#endif
#if NET9_0_OR_GREATER
            _staticRuntimePreprocessorSymbols.Add("NET9_0_OR_GREATER", true);
#endif
#if NET10_0
            _staticRuntimePreprocessorSymbols.Add("NET10_0", true);
#endif
#if NET10_0_OR_GREATER
            _staticRuntimePreprocessorSymbols.Add("NET10_0_OR_GREATER", true);
#endif
        }
        /// <summary>
        /// Creates a new instance of WorkerInitOptions
        /// </summary>
        public WorkerInitOptions()
        {
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
            RuntimePreprocessorSymbols = DefaultRuntimePreprocessorSymbols.ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Sets the root url of the application that starts the worker.
        /// </summary>
        /// <remarks>
        /// This is used to resolve url's to the binaries needed to start the process. 
        /// You normally don't need to set this property. 
        /// If not set, resolves to <a href="https://developer.mozilla.org/en-US/docs/Web/API/HTMLBaseElement/href"> base.href</a> if a base tag is present in the DOM of the hosting application, 
        /// or falls back to <a href="https://developer.mozilla.org/en-US/docs/Web/API/Location/origin"> window.location.origin</a>.
        /// </remarks>
        public string AppRoot { get; set; }

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
        /// Removes given sections (described by a path, separated with '.') from the blazor.boot.json provided to the web worker. Experts only.
        /// </summary>
        /// <remarks>
        /// The following removes the libraryInitializers section.<br />
        /// <code>["resources.libraryInitializers"]</code>
        /// </remarks>
        public string[] PruneBlazorBootConfig { get; set; }

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
        /// Mono-wasm-annotated endpoint for doing callbacks on self invocations from the worker. Experts only.
        /// </summary>
        public string EndInvokeCallBackEndpoint { get; set; }

        /// <summary>
        /// Runtime-version specific preprocessor symbols that are available at runtime for the worker script.
        /// </summary>
        /// <remarks>https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives</remarks>
        public Dictionary<string, bool> RuntimePreprocessorSymbols { get; set; }

        /// <summary>
        /// Sets environment variables in the target worker.
        /// </summary>
        /// <remarks>
        /// Defaults to a single entry: DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '0'.
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public Dictionary<string, string> EnvMap { get; set; }
            = new Dictionary<string, string>() {
                { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "0" },
            };

        /// <summary>
        /// If set to <c>true</c>, enables fingerprinting and loads the importmap on initialization. 
        /// If configured elsewhere in blazor but not here, the dotnet.js file may fail to load.
        /// </summary>
        /// <remarks>For more information see https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/static-files</remarks>
        public bool UseFingerprinting { get; set; } = false;

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

            var pruneBlazorBootConfig = this.PruneBlazorBootConfig ?? Array.Empty<string>();
            pruneBlazorBootConfig = pruneBlazorBootConfig
                .Concat(initOptions.PruneBlazorBootConfig ?? Array.Empty<string>())
                .Distinct().ToArray();

            return new WorkerInitOptions
            {
                AppRoot = initOptions.AppRoot ?? this.AppRoot,
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
                EndInvokeCallBackEndpoint = initOptions.EndInvokeCallBackEndpoint ?? this.EndInvokeCallBackEndpoint,
                PruneBlazorBootConfig = pruneBlazorBootConfig,
                Debug = initOptions.Debug || this.Debug,
                UseFingerprinting = initOptions.UseFingerprinting || this.UseFingerprinting,
                EnvMap = newEnvMap,
            };
        }
    }

    public class MethodIdentifier
    {
        public string AssemblyName { get; set; }
        public string FullMethodName { get; set; }
    }
}