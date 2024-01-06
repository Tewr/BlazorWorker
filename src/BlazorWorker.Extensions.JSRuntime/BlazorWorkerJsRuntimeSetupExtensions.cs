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
    }
}
