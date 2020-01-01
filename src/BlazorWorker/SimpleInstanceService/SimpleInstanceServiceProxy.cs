using MonoWorker.Core;
using MonoWorker.Core.SimpleInstanceService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWorker.Core.SimpleInstanceService
{
    class SimpleInstanceServiceProxy : ISimpleInstanceService
    {
        private IWorkerMessageService source;
        private Dictionary<long, TaskCompletionSource<DisposeResult>> disposeResultSourceByCallId = 
            new Dictionary<long, TaskCompletionSource<DisposeResult>>();
        private Dictionary<long, TaskCompletionSource<InitInstanceResult>> initInstanceResultByCallId =
            new Dictionary<long, TaskCompletionSource<InitInstanceResult>>();
        private long callIdSource;

        public SimpleInstanceServiceProxy(IWorkerMessageService source)
        {
            this.source = source;
            this.source.IncomingMessage += OnIncomingMessage;
        }

        private void OnIncomingMessage(object sender, string message)
        {
            if (DisposeResult.CanDeserialize(message)) {
                var result = DisposeResult.Deserialize(message);
                if (disposeResultSourceByCallId.TryGetValue(result.CallId, out var taskCompletionSource))
                {
                    taskCompletionSource.SetResult(result);
                }
                return;
            }

            if (InitInstanceResult.CanDeserialize(message))
            {
                var result = InitInstanceResult.Deserialize(message);
                if (initInstanceResultByCallId.TryGetValue(result.CallId, out var taskCompletionSource))
                {
                    taskCompletionSource.SetResult(result);
                }
                return;
            }
        }

        public async Task<DisposeResult> DisposeInstance(DisposeInstanceRequest request)
        {
            request.CallId = ++callIdSource;
            var res = new TaskCompletionSource<DisposeResult>();
            disposeResultSourceByCallId.Add(request.CallId, res);
            await this.source.PostMessageAsync(request.Serialize());
            return await res.Task;
        }

        public async Task<InitInstanceResult> InitInstance(InitInstanceRequest request)
        {
            request.CallId = ++callIdSource;
            var res = new TaskCompletionSource<InitInstanceResult>();
            initInstanceResultByCallId.Add(request.CallId, res);
            await this.source.PostMessageAsync(request.Serialize());
            return await res.Task;
        }
    }
}
