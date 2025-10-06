using BlazorWorker.Demo.CustomElements.Components;
using BlazorWorker.Demo.Shared;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.RegisterCustomElement<SimpleWorker>("simple-worker");
builder.Services.AddDemoDependencies();

await builder.Build().RunAsync();
