using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWorker
{

    public class WebWorkerFactory
    {
        public static ProxyGenerator proxyGenerator = new ProxyGenerator();
        private WebWorkerOptions options;

        public WebWorkerFactory()
        {
            this.options = new WebWorkerOptions();
        }

        public WebWorker<T> Create<T>() where T: class
        {
            return new WebWorker<T>(proxyGenerator, options);
        }
    }

    public class WebWorker<T> : IDisposable where T : class
    {
        private ProxyGenerator proxyGenerator;
        private readonly WebWorkerOptions options;

        public WebWorker(ProxyGenerator proxyGenerator, WebWorkerOptions options)
        {
            this.proxyGenerator = proxyGenerator;
            this.options = options;
        }
        public T CreateInstance() {
            return this.proxyGenerator.CreateClassProxy<T>(new WebWorkerInstance<T>(options));
        }

        public void Dispose()
        {
            // Destroy WebWorker
        }
    }

    public class WebWorkerInstance<T> : IInterceptor
    {
        public WebWorkerInstance(WebWorkerOptions options)
        {
            this.options = options;
        }

        private WebWorkerOptions options { get; }

        public void Intercept(IInvocation invocation)
        {
            var methodCall = new
            {
                invocation.Method.Name,
                arguments = invocation.Arguments.Select(arg => options.Serializer.Serilize(arg)).ToList(),
                gemericArguments = invocation.GenericArguments.Select(typeArg => typeArg.FullName).ToList()
            };

            
        }
    }

    public class WebWorkerOptions
    {
        public ISerializer Serializer { get; set; }

        public IJSRuntime {get;set;}
    }
}
