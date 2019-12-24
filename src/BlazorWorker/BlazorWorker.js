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
        console.debug("BlazorWorker.js:16:workerDef - initConf", initConf);
        var config = {};
        var Module = {};
        config.file_list = [];

        const fetch_file_cb = function (asset) {

            const fetchOverride = initConf.FetchOverride[asset];
            if (fetchOverride) {

                const resolve_func2 = function (resolve, reject) {
                    
                    const raw = self.atob(fetchOverride.base64Data);
                    const rawLength = raw.length;
                    const arrayBuffer = new ArrayBuffer(rawLength);
                    const writableBuffer = new Uint8Array(arrayBuffer);

                    for (i = 0; i < rawLength; i++) {
                        writableBuffer[i] = raw.charCodeAt(i);
                    }
                    
                    resolve(arrayBuffer);
                };

                const resolve_func1 = function (resolve, reject) {
                    const response = {
                        ok: true,
                        url: fetchOverride.url,
                        arrayBuffer: function () {
                            return new Promise(resolve_func2);
                        }
                    };
                    resolve(response);
                };
                return new Promise(resolve_func1);
            }
            return fetch(asset, {
                credentials: "same-origin"
            });
        };

        
        global = globalThis;
        Module = self.Module = {
            onRuntimeInitialized: function () {
                MONO.mono_load_runtime_and_bcl(
                    'app', //config.vfs_prefix,
                    `${initConf.appRoot}/${initConf.deploy_prefix}`, //config.deploy_prefix,
                    true, //config.enable_debugging
                    initConf.DependentAssemblyFilenames,
                    function () {
                        console.debug("mono loaded.");
                        const messageHandler =
                            Module.mono_bind_static_method(initConf.MessageEndPoint);
                        // Future messages goes directly to the message handler
                        self.onmessage = msg => {
                            messageHandler(msg.data);
                        };  
                        
                        if (!initConf.InitEndPoint) {
                            return;
                        }

                        try {
                            Module.mono_call_static_method(initConf.InitEndPoint, []);
                        } catch (e) {
                            console.error(`Init method ${initConf.InitEndPoint} failed`, e);
                            throw e;
                        }
                    },
                    fetch_file_cb
                );
            },
            locateFile: function (path, scriptDirectory) {
                const fileParts = (path || '').split("/");
                const fileName = fileParts[fileParts.length - 1];
                if (initConf.FetchUrlOverride[fileName]) {
                    return initConf.FetchUrlOverride[fileName];
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
        console.debug("initWorker", id, callbackInstance, initOptions);
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substr(0, appRoot.length - 1);
        }

        const initConf = {
            appRoot: appRoot,
            DependentAssemblyFilenames: initOptions.dependentAssemblyFilenames,
            FetchUrlOverride: initOptions.fetchUrlOverride,
            deploy_prefix: "_framework/_bin",
            MessageEndPoint: initOptions.messageEndPoint,
            InitEndPoint: initOptions.initEndPoint,
            wasmRoot: "_framework/wasm",
            FetchOverride: initOptions.fetchOverride
        };
        // Initialize worker
        const renderedConfig = JSON.stringify(initConf).replace('$appRoot$', appRoot);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const worker = new Worker(URL.createObjectURL(blob));
        workers[id] = worker;

        worker.onmessage = function (ev) {
            //const message = JSON.stringify({ id, data: ev.data });
            console.debug(`BlazorWorker.js:worker->blazor`, initOptions.callbackMethod, ev.data);
            callbackInstance.invokeMethod(initOptions.callbackMethod, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        console.debug('window:postMessage', workerId, message);
        workers[workerId].postMessage(message);
    };

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();