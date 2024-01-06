using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace BlazorWorker.Demo.IoCExample
{
    public static class ServiceCollectionHelper
    {
        public delegate void Configure(IServiceCollection services);

        public static IServiceProvider BuildServiceProviderFromMethod(Configure configureMethod)
        {
            var serviceCollection = new ServiceCollection();
            configureMethod(serviceCollection);
            return serviceCollection.BuildServiceProvider();
        }

        /*public static IServiceCollection AddBlazorWorkerJsRuntime(this IServiceCollection source)
        {
            source.AddSingleton(_ => JSRuntimeFactory());
            return source;
        }

        /// <summary>
        /// Loads <see cref="IJSRuntime"/> from private classes in the framework
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IJSRuntime JSRuntimeFactory()
        {
            var typeName = "Microsoft.AspNetCore.Components.WebAssembly.Services.DefaultWebAssemblyJSRuntime";
            var sourceAssembly = typeof(Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment).Assembly;
            var defaultWebAssemblyJSRuntimeType = 
                sourceAssembly.GetTypes().Where(x => x.FullName == typeName).FirstOrDefault()
                    ?? throw new Exception($"Unable to find type {typeName} in assembly {sourceAssembly}");
            var instanceProperty = defaultWebAssemblyJSRuntimeType.GetField("Instance", BindingFlags.Static | BindingFlags.NonPublic) 
                ?? throw new Exception($"Unable to find property 'Instance' of {typeName}");
            if (instanceProperty.GetValue(null) is not IJSRuntime propertyValue)
            {
                throw new Exception($"Unable to get field value for property 'Instance' of {typeName} as {nameof(IJSRuntime)}");
            }

            return propertyValue;
        }*/
    }
}
