[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.BackgroundService.svg?label=Tewr.BlazorWorker.BackgroundService)](https://www.nuget.org/packages/Tewr.BlazorWorker.BackgroundService)
[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.Core.svg?label=Tewr.BlazorWorker.Core)](https://www.nuget.org/packages/Tewr.BlazorWorker.Core)
[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.Extensions.JSRuntime.svg?label=Tewr.BlazorWorker.Extensions.JSRuntime)](https://www.nuget.org/packages/Tewr.BlazorWorker.Extensions.JSRuntime)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=AC77J8GFQ6LYA&item_name=BlazorWorker+Project&currency_code=EUR&source=url)
<p align="center">
  <img width="150" height="150" src="icon.svg" align="right">
</p>

# BlazorWorker
Library that provides a simple API for exposing dotnet [web workers](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Using_web_workers) in Client-side [Blazor](https://github.com/dotnet/aspnetcore/tree/master/src/Components#blazor).

Checkout the [Live demo](https://tewr.github.io/BlazorWorker) to see the library in action.

This library is useful for
- CPU-intensive tasks that merit parallel execution without blocking the UI
- Executing code in an isolated process

Web workers, simply speaking, is a new process in the browser with a built-in message bus. 

To people coming from the .NET world, an analogy for what this library does is calling [Process.Start](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start) to start a new .NET process, and expose a message bus to communicate with it.

The library comes in two flavours, one built on top of the other:
- BlazorWorker.BackgroundService: A high-level API that hides the complexity of messaging
- BlazorWorker.Core: A low-level API to communicate with a new .NET process in a web worker

### Net 5 & 6 Support
.netstandard2, .net5 and .net6 can be used as targets with BlazorWorker version v3.x, but new features will not be developed for these targets dues to the breaking changes in .net7.

### Net 7 & 8 Support
.net7 & .net8 targets can be used from [release v4.0.0](https://github.com/Tewr/BlazorWorker/releases/tag/v4.0.0) and higher versions.

### Native framework multithreading
Multi-threading enthusiasts should closely monitor [this tracking issue](https://github.com/dotnet/runtime/issues/68162) in the dotnet runtime repo, which promises experimental threading support in ~~.net 7 (projected for november 2022)~~ ~~.net8, projected for November 2023.~~ .net9, projected for November 2024.

.net7-rc2 has an experimental multithreading api, read about it [here](https://devblogs.microsoft.com/dotnet/asp-net-core-updates-in-dotnet-7-rc-2/#webassembly-multithreading-experimental)



## Installation
Nuget package:
```
Install-Package Tewr.BlazorWorker.BackgroundService
```

Add the following line in `Program.cs`:

```cs
  builder.Services.AddWorkerFactory();
```

And then in a `.razor` View:
```cs
@using BlazorWorker.BackgroundServiceFactory
@using BlazorWorker.Core
@inject IWorkerFactory workerFactory
```

## BlazorWorker.BackgroundService
A high-level API that abstracts the complexity of messaging by exposing a strongly typed interface with [Expressions](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression). Mimics [Task.Run](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run) as closely as possible to enable multi-threading.

The starting point of a BlazorWorker is a service class that must be defined by the caller. The public methods that you expose in your service can then be called from the IWorkerBackgroundService interface. If you declare a public [event](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/event) on your service, it can be used to call back into blazor during a method execution (useful for progress reporting).

Each worker process can contain multiple service classes, but each single worker can work with only one thread. For multiple concurrent threads, you must create a new worker for each (see the [Multithreading example]( https://tewr.github.io/BlazorWorker/BackgroundServiceMulti) for a way of organizing this.

Example (see the [demo project](src/BlazorWorker.Demo/Client/Pages) for a fully working example):
```cs
// MyCPUIntensiveService.cs
public class MyCPUIntensiveService {
  public int MyMethod(int parameter) {
    while(i < 5000000) i += (i*parameter);
    return i;
  }
}
```

```cs
// .razor view
@using BlazorWorker.BackgroundServiceFactory
@using BlazorWorker.Core
@inject IWorkerFactory workerFactory

<button @onclick="OnClick">Test!</button>
@code {
    int parameterValue = 5;
    
    public async Task OnClick(EventArgs _)
    {
        // Create worker.
        var worker = await workerFactory.CreateAsync();
        
        // Create service reference. For most scenarios, it's safe (and best) to keep this 
        // reference around somewhere to avoid the startup cost.
        var service = await worker.CreateBackgroundServiceAsync<MyCPUIntensiveService>();
        
        // Reference that live outside of the current scope should not be passed into the expression.
        // To circumvent this, create a scope-local variable like this, and pass the local variable.
        var localParameterValue = this.parameterValue;
        var result = await service.RunAsync(s => s.MyMethod(localParameterValue));
    }
}

```


## Setup dependencies [DEPRECATED FROM 4.0.0 and onwards]

_While still technically available, these apis are not used by the library since version 4.0.0 and should be removed from your code. They will be completely reomved in a future release._

By default, `worker.CreateBackgroundServiceAsync<MyService>()` will try to guess the name of the dll that `MyService` resides in (it is usually AssemblyNameOfMyService.dll).

If your dll name does not match the name of the assembly, or if your service has additional dependencies, you must provide this information in `WorkerInitOptions`. If `WorkerInitOptions` is provided, the default options are no longer created, so you also have to provide the dll `MyService` resides in (even if it is in AssemblyNameOfMyService.dll). Examples:

```cs
  // Custom service dll, additional dependency, using dll names
  var serviceInstance = await worker.CreateBackgroundServiceAsync<MyService>(
      options => options.AddAssemblies("MyService.dll", "MyServiceDependency.dll")
  );
      
  // Default service dll, additional dependency with dll deduced from assembly name of provided type
  var serviceInstance2 = await worker.CreateBackgroundServiceAsync<MyService>(
      options => options
          .AddConventionalAssemblyOfService()
          .AddAssemblyOf<TypeOfMyServiceDependency>()
  );
  
  // In addition to default service dll, add HttpClient as Dependency (built-in dependency definition / helper)
  var serviceInstance3 = await worker.CreateBackgroundServiceAsync<MyService>(
      options => options
          .AddConventionalAssemblyOfService()
          .AddHttpClient()
  );
```

## More Culture!

Since .net6.0, the runtime defaults to the invariant culture, and new cultures cannot be used or created by default. You [may](https://github.com/Tewr/BlazorWorker/issues/67) get the exception with the message "Only the invariant culture is supported in globalization-invariant mode", commonly when using third-party libraries that make use of any culture other than the invariant one.

You may try to circument any problems relating to this by changing the default options.

```cs
  var serviceInstance4 = await worker.CreateBackgroundServiceAsync<MyService>(
      options => options
          .AddConventionalAssemblyOfService()
          // Allow custom cultures by setting this to zero
          .SetEnv("DOTNET_SYSTEM_GLOBALIZATION_PREDEFINED_CULTURES_ONLY", "0")
  );
```

Read more [here](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_system_globalization_) on culture options.


## Injectable services
The nominal use case is that the Service class specifies a parameterless constructor.

If provided as constructor parameters, any of the two following services will be created and injected into the service: 

* <code>HttpClient</code> - use to make outgoing http calls, like in blazor. <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient">Reference</a>.
* <code>IWorkerMessageService</code> - to communicate with the worker from blazor using messages, the lower-most level of communication. Accepts messages from <code>IWorker.PostMessageAsync</code>, and provides messages using <code>IWorker.IncomingMessage</code>. See the <a href="src/BlazorWorker.Demo/SharedPages/Pages/CoreExample.razor">Core example</a> for a use case.

These are the only services that will be injected. Any other custom dependencies has to be automated in some other way. Two extension methods simplify this by exposing the factory pattern which can be implemented with a container of your choice: <code>IWorker.CreateBackgroundServiceUsingFactoryAsync<TFactory, TService></code> and <code>IWorkerBackgroundService<TFactory>.CreateBackgroundServiceAsync<TFactory, TService></code>. 
  
For an example of a full-fledged IOC Setup using <code>Microsoft.Extensions.DependencyInjection</code> see the <a href="src/BlazorWorker.Demo/SharedPages/Pages/IoCExamplePage.razor">IOC example</a>.


## Core package
[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.Core.svg?label=Tewr.BlazorWorker.Core)](https://www.nuget.org/packages/Tewr.BlazorWorker.Core)

The Core package does not provide any serialization. This is useful for scenarios with simple API's (smaller download size), or for building a custom high-level API. See the <a href="src/BlazorWorker.Demo/SharedPages/Pages/CoreExample.razor">Core example</a> for a use case.

## Extensions.JSRuntime package
[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.Extensions.JSRuntime.svg?label=Tewr.BlazorWorker.Extensions.JSRuntime)](https://www.nuget.org/packages/Tewr.BlazorWorker.Extensions.JSRuntime)

The JSRuntime package has primarily been developed as a middleware for supporting IndexedDB, more specifically the package [Tg.Blazor.IndexedDB](https://github.com/wtulloch/Blazor.IndexedDB).  See the <a href="src/BlazorWorker.Demo/SharedPages/Pages/IndexedDb.razor">IndexedDb example</a> for a use case.
