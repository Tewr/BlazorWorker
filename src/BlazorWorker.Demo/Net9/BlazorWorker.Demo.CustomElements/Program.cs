using BlazorWorker.Demo.Shared;
using BlazorWorker.Demo.SharedPages.Pages;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.RegisterCustomElement<SimpleWorkerCustomElements>("simple-worker");
builder.Services.AddDemoDependencies();

await builder.Build().RunAsync();
