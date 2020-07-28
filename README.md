[![NuGet](https://img.shields.io/nuget/dt/Tewr.BlazorWorker.BackgroundService.svg?label=Tewr.BlazorWorker.BackgroundService)](https://www.nuget.org/packages/Tewr.BlazorWorker.BackgroundService)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=AC77J8GFQ6LYA&item_name=BlazorWorker+Project&currency_code=EUR&source=url)

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


## Setup dependencies

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

## Injectable services
The nominal use case is that the Service class specifies a parameterless constructor.

If provided as constructor parameters, any of the two following services will be created and injected into the service: 

* <code>HttpClient</code> - use to make outgoing http calls, like in blazor. <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient">Reference</a>.
* <code>IWorkerMessageService</code> - to communicate with the worker from blazor using messages, the lower-most level of communication. Accepts messages from <code>IWorker.PostMessageAsync</code>, and provides messages using <code>IWorker.IncomingMessage</code>. See the <a href="src/BlazorWorker.Demo/Client/Pages/CoreExample.razor">Core example</a> for a use case.
