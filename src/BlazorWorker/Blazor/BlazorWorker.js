window.BlazorWorker = function () {
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const workerDef = function (firstMessage) {
        const appRoot = "$appRoot$";

        var config = {};
        var Module = {};
        config.file_list = [];

        // get blazor boot json for file list;
        fetch(`${appRoot}/_framework/blazor.boot.json`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json'
            }
        }).then(function (promise) {
            promise.json().then(function (response) {
                Module = self.Module = {
                    onRuntimeInitialized: function () {
                        MONO.mono_load_runtime_and_bcl(
                            'app', //config.vfs_prefix,
                            `${appRoot}/_framework/_bin`, //config.deploy_prefix,
                            true, //config.enable_debugging
                            response.assemblyReferences,
                            function () {
                                const messageHandler = Module.mono_bind_static_method("[BlazorWorker] Worker.Instance.OnMessage");
                                self.onmessage = msg => {
                                    messageHandler(msg.data);
                                };

                                // Treat the first message immediately
                                messageHandler(firstMessage.data);
                            }
                        );
                    },
                    locateFile: function (path, scriptDirectory) {
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

    const initWorker = function (id, callbackInstance) {
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
        const callBack = Blazor.platform.findMethod("BlazorWorker", "BlazorWorker.Blazor", "WebWorkerProxy", "OnMessage");

        worker.onmessage = function (ev) {
            Blazor.platform.callMethod(callBack, callbackInstance, id, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        workers[workerId].postMessage(message);
    };

    

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();