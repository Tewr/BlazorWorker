using BlazorWorker.Core;
using BlazorWorker.WorkerCore;
using System;
using System.Threading.Tasks;
using BlazorWorker.BackgroundServiceFactory;
using Microsoft.JSInterop;
using BlazorWorker.WorkerBackgroundService;


namespace BlazorWorker.Demo.Shared
{
    public partial class JsDirectExample
    {
        private readonly IWorkerFactory workerFactory;
        private readonly IJSRuntime jsRuntime;
        private IWorkerBackgroundService<JsDirectExampleWorkerService> service;
        private long workerId;
        public event EventHandler<string> LogHandler;

        public JsDirectExample(IWorkerFactory workerFactory, IJSRuntime jsRuntime)
        {
            this.workerFactory = workerFactory;
            this.jsRuntime = jsRuntime; 
        }

        private void Log(string message)
        {
            LogHandler?.Invoke(this, message);
        }

        public async Task Execute()
        {
            if (this.service == null)
            {
                Log("Execute: Creating worker...");
                var worker = await this.workerFactory.CreateAsync();
                this.workerId = worker.Identifier;
                Log("Execute: Creating service...");
                this.service = await worker.CreateBackgroundServiceAsync<JsDirectExampleWorkerService>();

                Log("Execute: Creating script on main js...");

                // this could (should) be defined in a javascript file included in your app
                var myJavascript = @"
const output = document.getElementById('jsDirectOutputElement');
output.innerText += `\nSetting up event listener on window for event blazorworker_jsdirect.`;

window.addEventListener('blazorworker_jsdirect', function(e) {
    if (e.detail.workerId === " + this.workerId + @") {
        console.log('blazorworker_jsdirect handler!', {data: e.detail.data});
        
        output.innerText += `\nblazorworker_jsdirect handler detail data: '${e.detail.data}'`;
    }
    else {
        console.log('blazorworker_jsdirect handler for some other worker', { workerId: e.detail.workerId, data: e.detail.data});
    }
});
";

                await this.jsRuntime.InvokeVoidAsync("eval", myJavascript);
                Log("Execute: Init done.");
            }

            Log("Execute: Calling ExecuteJsDirect on worker...");
            await service.RunAsync(s => s.ExecuteJsDirect());
            Log("Execute: Done");
        }



    }

    public class JsDirectExampleWorkerService
    {
        private readonly IWorkerMessageService messageService;
        public JsDirectExampleWorkerService(IWorkerMessageService messageService)
        {
            this.messageService = messageService;
        }
        public async Task ExecuteJsDirect()
        {
            await messageService.PostMessageJsDirectAsync("This message goes directly to main js thread.");
        }
    }
}
