using BlazorWorker.Core;
using BlazorWorker.Demo.Client;
//using BlazorWorker.Demo.IoCExample;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddWorkerFactory();
//builder.Services.AddIndexedDbDemoPersonConfig();

await builder.Build().RunAsync();
