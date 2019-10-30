using BlazorWorker.Shared;
using Microsoft.JSInterop;
using Serialize.Linq.Extensions;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.Blazor
{
    public class WebWorkerServiceProxy<T> : IWorkerService<T> where T : class
    {
        private readonly string workerGuid;
        private readonly WebWorkerOptions options;
        private readonly IJSRuntime jsRuntime;
        
        private string guid = Guid.NewGuid().ToString("n");


        public WebWorkerServiceProxy(
            string workerGuid,
            WebWorkerOptions options, 
            IJSRuntime jsRuntune)
        {
            this.workerGuid = workerGuid;
            this.options = options;
            this.jsRuntime = jsRuntune;
        }

        public async Task InitAsync()
        {
            
            await jsRuntime.InvokeVoidAsync("BlazorWorker.postMessage",
                this.workerGuid,
                this.options.Serializer.Serialize(
                    new InitInstanceParams() { 
                        WorkerId = this.workerGuid, 
                        InstanceId = guid,
                        AssemblyName = typeof(T).Assembly.FullName,
                        TypeName = typeof(T).FullName
                    }));
        }

        public async Task InvokeVoidAsync(Expression<Action<T>> action)
        {
            var methodCall = GetCall(action);

            await jsRuntime.InvokeVoidAsync("BlazorWorker.methodCallVoid", methodCall);
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<T, TResult>> action)
        {
           var methodCall = GetCall(action);

           return await jsRuntime.InvokeAsync<TResult>("BlazorWorker.postMessage",
                this.workerGuid,
                methodCall);

            //return await jsRuntime.InvokeAsync<TResult>("BlazorWorker.methodCall", methodCall);
        }

        private string GetCall(Expression action)
        {
            var args = action.ToExpressionNode();

            var methodCall = new InstanceMethodCallParams
            {
                WorkerId = workerGuid,
                InstanceId = guid,
                MethodCall = args
            };

            return (this.options.Serializer ?? DefaultSerializer.Instance).Serialize(methodCall);
        }
    }
}
