[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=AC77J8GFQ6LYA&item_name=BlazorWorker+Project&currency_code=EUR&source=url)

# BlazorWorker
Library that provides a simple API for exposing dotnet [web workers](https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API/Using_web_workers) in Client-side Blazor.

This library is useful for
- CPU-intensive tasks that merit parallel execution without blocking the UI
- Executing code in an isolated process

Web workers, simply speaking, is a new process in the browser with a built-in message bus. 

To people coming from the .NET world, an analogy for what this library does is calling [Process.Start](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.start) to start a new .NET process, and expose a message bus to communicate with it.

The library comes in two flavours, one built on top of the other:
- BlazorWorker.BackgroundService: A high-level API that hides the complexity of messaging
- BlazorWorker.WorkerCore: A low-level API to communicate with a new .NET process in a web worker

## Installation
Add the following line in `Program.cs`:

```cs
  builder.Services.AddWorkerFactory();
```

And then in a `.razor` View:
```cs
@inject IWorkerFactory workerFactory
```

## BlazorWorker.BackgroundService
A high-level API that abstracts the complexity of messaging by exposing a strongly typed interface with [Expressions](https://docs.microsoft.com/en-us/dotnet/api/system.linq.expressions.expression). Mimic [Task.Run](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.run) as closely as possible to enable multi-threading.

The starting point of a BlazorWorker is a service class that must be defined by the caller. The public methods that you expose in your service can then be called from the IWorkerBackgroundService interface. If you declare a public [event](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/event) on your service, it can be used to call back into blazor during a method execution (useful for progress reporting).

Each worker process can contain multiple service classes. 

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
@inject IWorkerFactory workerFactory
@using BlazorWorker.BackgroundServiceFactory

<button @onclick="OnClick">Test!</button>
@code {

    public async Task OnClick(EventArgs _)
    {
        // Create worker.
        var worker = await workerFactory.CreateAsync();
        
        // Create service reference. For most scenarios, it's safe (and best) to keep this 
        // reference around somewhere to avoid the startup cost.
        var service = await worker.CreateBackgroundServiceAsync<MyCPUIntensiveService>();
        
        var result = await service.RunAsync(s => s.MyMethod(5));
    }
}

```
