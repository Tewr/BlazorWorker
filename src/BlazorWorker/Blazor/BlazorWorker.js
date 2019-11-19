window.BlazorWorker = function () {
    
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const workerDef = function (initMessage) {

        console.debug("BlazorWorker.js:16:workerDef - initMessage", initMessage);
        const appRoot = "$appRoot$";
        
        const staticAssemblyRefs = [
            //"BlazorWorker.Demo.Shared.dll",
            // "BlazorWorker.dll",
            "MonoWorker.Core.dll",
            //"Microsoft.AspNetCore.Blazor.dll",
            //"Microsoft.AspNetCore.Blazor.HttpClient.dll",
            //"Microsoft.AspNetCore.Components.dll",
            //"Microsoft.AspNetCore.Components.Forms.dll",
            //"Microsoft.AspNetCore.Components.Web.dll",
            //"Microsoft.Bcl.AsyncInterfaces.dll",
            //"Microsoft.Extensions.DependencyInjection.Abstractions.dll",
            //"Microsoft.Extensions.DependencyInjection.dll",
            //"Microsoft.Extensions.Logging.Abstractions.dll",
            //"Microsoft.Extensions.Options.dll",
            //"Microsoft.Extensions.Primitives.dll",
            //"Microsoft.JSInterop.dll",
            //"Mono.Security.dll",
            // "Mono.WebAssembly.Interop.dll",
            "netstandard.dll",
            "mscorlib.dll",
            //"Newtonsoft.Json.dll",
            //"Serialize.Linq.dll",
            //"System.Buffers.dll",
            //"System.ComponentModel.Annotations.dll",
            "System.Core.dll",
            //"System.Data.dll",
            "System.dll",
            //"System.Memory.dll",
            //"System.Net.Http.dll",
            //"System.Numerics.dll",
            //"System.Numerics.Vectors.dll",
            //"System.Runtime.CompilerServices.Unsafe.dll",
            //"System.Runtime.Serialization.dll",
            //"System.ServiceModel.Internals.dll",
            //"System.Text.Encodings.Web.dll",
            //"System.Text.Json.dll",
            //"System.Threading.Tasks.Extensions.dll",
            //"System.Xml.dll",
            //"System.Xml.Linq.dll",
            "WebAssembly.Bindings.dll",
            //"BlazorWorker.Demo.Client.pdb",
            //"BlazorWorker.Demo.Shared.pdb",
            //"BlazorWorker.pdb",
            //"WebAssembly.Bindings.pdb"
        ];

        const assemblyRedirectByFilename = {
            "WebAssembly.Bindings.dll": `${appRoot}/WebAssembly.Bindings.0.2.2.0.dll`,
           // "MonoWorker.Core.dll": `${appRoot}/MonoWorker.Core.dll`
        };

        var config = {};
        var Module = {};
        config.file_list = [];

        // get blazor boot json for file list;
        fetch(`${appRoot}/_framework/blazor.boot.json`,
 {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(function (promise) {
            promise.json().then(function (response) {
                global = globalThis;
                Module = self.Module = {
                    onRuntimeInitialized: function () {
                        MONO.mono_load_runtime_and_bcl(
                            'app', //config.vfs_prefix,
                            `${appRoot}/_framework/_bin`, //config.deploy_prefix,
                            true, //config.enable_debugging
                            staticAssemblyRefs,
                            //response.assemblyReferences,
                            function () {
                                console.debug("mono loaded.");
                                const messageHandler = Module.mono_bind_static_method("[MonoWorker.Core]MonoWorker.Core.MessageService:OnMessage");
                                // Future messages goes directly to the message handler
                                self.onmessage = msg => {
                                    messageHandler(msg.data);
                                };

                                // Treat the first message immediately
                                messageHandler(initMessage || '');
                            }
                        );
                    },
                    locateFile: function (path, scriptDirectory) {
                        const fileParts = (path || '').split("/");
                        const fileName = fileParts[fileParts.length - 1];
                        if (assemblyRedirectByFilename[fileName]) {
                            return assemblyRedirectByFilename[fileName];
                        }
                        if (path.startsWith(appRoot)) {
                            return path;
                        }

                        scriptDirectory = scriptDirectory || `${appRoot}/_framework/wasm/`;
                        return scriptDirectory + path;
                    }
                };
                
                self.importScripts(`${appRoot}/_framework/wasm/mono.js`);
            });
        }, function (error) {
            console.log(error);
        });
        
    };

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = function (id, callbackInstance, initOptions) {
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substr(0, appRoot.length - 1);
        }
        // Initialize worker
        const renderedInlineWorker = inlineWorker.replace('$appRoot$', appRoot);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;

        // Setup callback message 
        // Initialize worker
        var first = true;
        worker.onmessage = function (ev) {
            const message = JSON.stringify({ id, data: ev.data });
            console.debug(`BlazorWorker.js:141: ${message}`);
            callbackInstance.invokeMethod('OnMessage', message);
            if (first) {
                first = false;
                console.debug("sending initOptions message", initOptions);
                worker.postMessage(JSON.stringify(initOptions));
            }
        };

    };

    const postMessage = function (workerId, message) {
        console.debug('window : postMessage', workerId, message);
        workers[workerId].postMessage(message);
    };

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();