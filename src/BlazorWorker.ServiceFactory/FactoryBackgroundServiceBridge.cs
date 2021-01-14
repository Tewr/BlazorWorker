using BlazorWorker.WorkerBackgroundService;
using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace BlazorWorker.BackgroundServiceFactory
{
    internal class FactoryBackgroundServiceBridge<TFactory, TService> : 
        IWorkerBackgroundService<TService>, IWorkerBackgroundServiceFactory<TService>
        where TFactory : class 
        where TService : class
    {
        private readonly IWorkerBackgroundService<TFactory> backgroundService;
        private readonly IWorkerBackgroundServiceFactory<TFactory> backgroundServiceFactory;
        private readonly Expression<Func<TFactory, TService>> factoryExpression;

        public FactoryBackgroundServiceBridge(
            IWorkerBackgroundService<TFactory> backgroundService, 
            Expression<Func<TFactory, TService>> bridgeExpression)
        {
            this.backgroundService = backgroundService ??
                throw new ArgumentNullException(nameof(backgroundService));
            if (!(backgroundService is IWorkerBackgroundServiceFactory<TFactory>))
            {
                throw new ArgumentException($"Only an instance of type {nameof(IWorkerBackgroundServiceFactory<TFactory>)} is supported.", nameof(backgroundService));
            }

            this.backgroundServiceFactory = (IWorkerBackgroundServiceFactory<TFactory>)backgroundService;

            this.factoryExpression = bridgeExpression ?? 
                throw new ArgumentNullException(nameof(bridgeExpression));

            Disposables = new List<IAsyncDisposable>();
        }

        public List<IAsyncDisposable> Disposables { get; }

        public Task<TResult> RunAsync<TResult>(Expression<Func<TService, TResult>> function) =>
            this.backgroundService.RunAsync(CombineExpressions(function));
        
        public Task RunAsync(Expression<Action<TService>> action) =>
            this.backgroundService.RunAsync(CombineExpressions(action));
        
        public Task<TResult> RunAsync<TResult>(Expression<Func<TService, Task<TResult>>> function) =>
            this.backgroundService.RunAsync(CombineExpressions(function));
        
        public Task RunAsync<TResult>(Expression<Func<TService, Task>> function) =>
            this.backgroundService.RunAsync(CombineExpressions(function));
        
        public Expression<Func<TFactory, TResult>> CombineExpressions<TResult>(Expression<Func<TService, TResult>> function)
        {
            var factory = Expression.Variable(typeof(TFactory), "f");
            var service = Expression.Invoke(this.factoryExpression, factory);
            var methodCall = Expression.Invoke(function, service);

            var expressionToSend = Expression.Lambda<Func<TFactory, TResult>>(methodCall, factory);

            return expressionToSend;
        }

        public Expression<Action<TFactory>> CombineExpressions(Expression<Action<TService>> function)
        {
            var f = Expression.Variable(typeof(TFactory), "f");
            var service = Expression.Invoke(this.factoryExpression, f);
            var methodCall = Expression.Invoke(function, service);

            var expressionToSend = Expression.Lambda<Action<TFactory>>(methodCall, f);

            return expressionToSend;
        }

        public Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler) =>
            backgroundServiceFactory.RegisterEventListenerAsync(eventName, myHandler, this.factoryExpression);

        public Task<EventHandle> RegisterEventListenerAsync<TResult>(string eventName, EventHandler<TResult> myHandler, Expression expression) =>
            backgroundServiceFactory.RegisterEventListenerAsync(eventName, myHandler, this.factoryExpression);

        #region Fully Delegated implementation

        public Task UnRegisterEventListenerAsync(EventHandle handle) => 
            backgroundService.UnRegisterEventListenerAsync(handle);

        IWorkerMessageService IWorkerBackgroundService<TService>.GetWorkerMessageService() 
            => backgroundService.GetWorkerMessageService();

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            foreach (var disposable in Disposables)
            {
                await disposable.DisposeAsync();
            }
        }

        #endregion

    }
}