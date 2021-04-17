using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorWorker.Extensions.JSRuntime
{
    public static class BlazorWorkerJsRuntimeSetupExtensions
    {
        public static IServiceCollection AddBlazorWorkerJsRuntime(this IServiceCollection source)
        {
            source.AddSingleton<IJSRuntime, BlazorWorkerJSRuntime>();
            return source;
        }
    }
}
