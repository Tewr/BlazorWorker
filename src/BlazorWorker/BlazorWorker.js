window.BlazorWorker = function () {
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker && worker.terminate) {
            worker.terminate();
            delete workers[workerId];
        }
    };

    const workerDef = function () {

        const initConf = JSON.parse('$initConf$');
        console.debug("BlazorWorker.js:18:workerDef - initConf", initConf);
        var config = {};
        var Module = {};
        config.file_list = [];

        // get blazor boot json for file list;
        
        global = globalThis;
        Module = self.Module = {
            onRuntimeInitialized: function () {
                MONO.mono_load_runtime_and_bcl(
                    'app', //config.vfs_prefix,
                    `${initConf.appRoot}/${initConf.deploy_prefix}`, //config.deploy_prefix,
                    true, //config.enable_debugging
                    initConf.staticAssemblyRefs,
                    function () {
                        console.debug("mono loaded.");
                        const messageHandler =
                            Module.mono_bind_static_method(initConf.messageEndPoint);
                        // Future messages goes directly to the message handler
                        self.onmessage = msg => {
                            messageHandler(msg.data);
                        };

                        // Treat the first message immediately 
                        // TODO: Remove, replace with postmessage(initDoneMessage)
                        messageHandler('INIT MESSAGE REMOVE ME');
                    }
                );
            },
            locateFile: function (path, scriptDirectory) {
                const fileParts = (path || '').split("/");
                const fileName = fileParts[fileParts.length - 1];
                if (initConf.assemblyRedirectByFilename[fileName]) {
                    return initConf.assemblyRedirectByFilename[fileName];
                }
                if (path.startsWith(initConf.appRoot)) {
                    return path;
                }

                scriptDirectory = scriptDirectory || `${initConf.appRoot}/${initConf.wasmRoot}/`;
                return scriptDirectory + path;
            }
        };
                
        self.importScripts(`${initConf.appRoot}/${initConf.wasmRoot}/mono.js`);
    };

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = function (id, callbackInstance, initOptions) {
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substr(0, appRoot.length - 1);
        }

        // TODO: move this or parts of this to dot-net land
        const initConf = {
            appRoot: appRoot,
            staticAssemblyRefs: initOptions.staticAssemblyRefs,
            assemblyRedirectByFilename: initOptions.assemblyRedirectByFilename,
            deploy_prefix: "_framework/_bin",
            messageEndPoint: initOptions.messageEndPoint,
            wasmRoot: "_framework/wasm"
        };
        // Initialize worker
        const renderedConfig = JSON.stringify(initConf).replace('$appRoot$', appRoot);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;

        // Setup callback message 
        // Initialize worker
        var first = true;
        worker.onmessage = function (ev) {
            const message = JSON.stringify({ id, data: ev.data });
            console.debug(`BlazorWorker.js:95: ${message}`);
            callbackInstance.invokeMethod(initOptions.callbackMethod, message);
            if (first) {
                first = false;
                console.debug("sending initOptions message", renderedConfig);
                worker.postMessage(renderedConfig);
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