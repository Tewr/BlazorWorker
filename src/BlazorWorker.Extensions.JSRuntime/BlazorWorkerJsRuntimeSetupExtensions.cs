using BlazorWorker.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorWorker.Extensions.JSRuntime
{
    public static class BlazorWorkerJsRuntimeSetupExtensions
    {
        public static IServiceCollection AddBlazorWorkerJsRuntime(this IServiceCollection source)
        {
            source.AddSingleton<IJSRuntime, BlazorWorkerJSRuntime>();
            source.AddSingleton<IBlazorWorkerJSRuntimeSerializer, DefaultBlazorWorkerJSRuntimeSerializer>();
            return source;
        }

        public static WorkerInitOptions AddBlazorWorkerJsRuntime(this WorkerInitOptions source)
        {
            source.AddAssemblyOf<Microsoft.JSInterop.IJSRuntime>()
                  .AddAssemblyOf<BlazorWorker.Extensions.JSRuntime.BlazorWorkerJSRuntime>()
                  .AddAssemblyOf<System.Text.Json.JsonElement>()
                  .AddAssemblyOf<System.Text.Encodings.Web.TextEncoderSettings>();
            return source;
        }
    }
}
