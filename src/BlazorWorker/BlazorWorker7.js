window.BlazorWorker = function () {
    
    const workers = {};
    const disposeWorker = function (workerId) {

        const worker = workers[workerId];
        if (worker) {
            if (worker.worker.terminate) {
                worker.worker.terminate();
            }
            URL.revokeObjectURL(worker.url);
            delete workers[workerId];
        }
    };

    const workerDef = function () {

        const initConf = JSON.parse('$initConf$');
        const blobRoot = self.location.href.split("/").slice(0, -1).join("/");
        const proxyLocation = { href: initConf.appRoot };
        const fetchHandler = {
            apply: function (target, thisArg, args) {
                args[0] = args[0].replace(blobRoot, initConf.appRoot);
                //console.log("BlazorWorker7::fetchHandler.apply", { target, thisArg, args });
                let fetchReturn = target.apply(thisArg, args);
                if (args[0].endsWith("mono-config.json")) {
                    // Override fetch result for "mono-config.json"
                    return Promise.race([new Response(JSON.stringify(self.blazorbootMonoConfig),
                        { "status": 200, headers: { "Content-Type": "application/json" } })]);
                }
                return fetchReturn;
            }
        }
        const proxyFetch = new Proxy(fetch, fetchHandler);
        self.fetch = proxyFetch;
        const handler = {
            get: function (target, prop, pxy) {
                if (prop == "location") {
                    return proxyLocation;
                }

                if (prop == "fetch") {
                    return proxyFetch;
                }

                return target[prop];
                
            }
        }
        self.window = new Proxy(self, handler);
        let messageHandler;
        const envMap = initConf.envMap;
        globalThis.ENV = globalThis.ENV || {};
        for (const key in envMap) {
            if (Object.prototype.hasOwnProperty.call(envMap, key)) {
                globalThis.ENV[key] = envMap[key];
                //MONO.mono_wasm_setenv(key, envMap[key]);
            }
        }
        //let endInvokeCallBack;
        const onReady = () => {
            /*endInvokeCallBack =
                BINDING.bind_static_method(initConf.endInvokeCallBackEndpoint);*/
            /*const messageHandler =
                BINDING.bind_static_method(initConf.MessageEndPoint);*/
            // Future messages goes directly to the message handler
            self.onmessage = msg => {
                messageHandler(msg.data);
            };

            /*if (!initConf.InitEndPoint) {
                return;
            }*/
            

            try {
                self.initMethod();
                /*let methodName = initConf.InitEndPoint;
                debugger;
                const initMethod = getChildFromDotNotation(`assemblyExports.${methodName}`);
                initMethod();*/
                //Module.mono_call_static_method(initConf.InitEndPoint, []);
            } catch (e) {
                console.error(`Init method ${initConf.InitEndPoint} failed`, e);
                throw e;
            }
        };


        function asyncLoad(url, reponseType) {
            return new Promise((resolve, reject) => {
                const xhr = new XMLHttpRequest();
                const arrayBufferType = 'arraybuffer';
                xhr.open('GET', url, /* async: */ true);
                xhr.responseType = reponseType || arrayBufferType;
                xhr.onload = function xhr_onload() {
                    if (xhr.status == 200 || xhr.status == 0 && xhr.response) {
                        if (this.responseType === arrayBufferType) {
                            const asm = new Uint8Array(xhr.response);
                            resolve(asm);
                        } else {
                            resolve(xhr.response);
                        }
                    } else {
                        reject(xhr);
                    }
                };
                xhr.onerror = reject;
                xhr.send(undefined);
            });
        }
        
        // reduce dot notation to last member of chain
        const getChildFromDotNotation = (member, root) =>
            member.split(".").reduce((m, prop) => Object.hasOwnProperty.call(m, prop) ? m[prop] : empty, root || self);

        //TODO: This call could/should be session cached. But will the built-in blazor fetch service worker override 
        // (PWA et al) do this already if configured ?
        asyncLoad(`${initConf.appRoot}/${initConf.blazorBoot}`, 'json')
            .then(async blazorboot => {

                const blazorbootMonoConfig = {
                    "mainAssemblyName": "BlazorWorker.WorkerCore.dll",
                    "assemblyRootFolder": "_framework",
                    "debugLevel": initConf.DebugLevel || -1,
                    "assets":
                        [...Object.keys(blazorboot.resources.assembly)
                            .concat(Object.keys(blazorboot.resources.pdb || {}) || []).map(dllName => {
                                return {
                                    "behavior": "assembly",
                                    "name": dllName
                                };
                            }),
                        ...[
                            /** todo: remove ? supportFiles directory does not exist*/
                            {
                                "virtualPath": "runtimeconfig.bin",
                                "behavior": "vfs",
                                "name": "supportFiles/0_runtimeconfig.bin"
                            },
                            /** todo: remove ? supportFiles directory does not exist*/
                            {
                                "virtualPath": "dotnet.js.symbols",
                                "behavior": "vfs",
                                "name": "supportFiles/1_dotnet.js.symbols"
                            },
                            {
                                "loadRemote": false,
                                "behavior": "icu",
                                "name": "icudt.dat"
                            },
                            {
                                "virtualPath": "/usr/share/zoneinfo/",
                                "behavior": "vfs",
                                "name": "dotnet.timezones.blat"
                            },
                            {
                                "behavior": "dotnetwasm",
                                "name": "_framework/dotnet.wasm"
                            }
                            ]],
                    "remoteSources": [],
                    "pthreadPoolSize": 0,
                    "generatedFromBlazorBoot":true
                };

                self.blazorbootMonoConfig = blazorbootMonoConfig;

                let dotnetjsfilename = '';
                const runttimeSection = blazorboot.resources.runtime;
                for (var p in runttimeSection) {
                    if (Object.prototype.hasOwnProperty.call(runttimeSection, p) && p.startsWith('dotnet.') && p.endsWith('.js')) {
                        dotnetjsfilename = p;
                    }
                }

                if (dotnetjsfilename === '') {
                    throw 'BlazorWorker: Unable to locate dotnetjs file in blazor boot config.';
                }

                //const { dotnet } = self.importScripts(`${initConf.appRoot}/${initConf.wasmRoot}/${dotnetjsfilename}`);
                const { dotnet } = await import(`${initConf.appRoot}/${initConf.wasmRoot}/${dotnetjsfilename}`);
                const { setModuleImports, getAssemblyExports, getConfig } = await dotnet
                    .withDiagnosticTracing(false)
                    .withApplicationArgumentsFromQuery()
                    .create();

                setModuleImports('BlazorWorker7.js', {
                    PostMessage: (messagecontent) => {
                        self.postMessage(messagecontent);
                    }
                });

                const config = getConfig();
                const exports = await getAssemblyExports(config.mainAssemblyName);
                // Save this info for later.
                //self.assemblyExports = exports;
                messageHandler = exports.BlazorWorker.WorkerCore.MessageService.OnMessage;
                const initExports = await getAssemblyExports("BlazorWorker.WorkerBackgroundService");
                self.initMethod = getChildFromDotNotation("BlazorWorker.WorkerBackgroundService.WorkerInstanceManager.Init", initExports)
                //endInvokeCallBack = exports.BlazorWorker.JSInvokeService.EndInvokeCallBack;
                await dotnet.run();
                onReady();
            
            }, errorInfo => console.error("error loading blazorboot", errorInfo));

        /*self.jsRuntimeSerializers = new Map();
        self.jsRuntimeSerializers.set('nativejson', {
            serialize: o => JSON.stringify(o),
            deserialize: s => JSON.parse(s)
        });*/

        const empty = {};



        // Async invocation with callback.
        /*
        self.beginInvokeAsync = async function (serializerId, invokeId, method, isVoid, argsString) {

            let result;
            let isError = false;
            let serializer = self.jsRuntimeSerializers.get(serializerId);
            if (!serializer) {
                result = `beginInvokeAsync: Unknown serializer with id '${serializerId}'`;
                serializer = self.jsRuntimeSerializers.get('nativejson');
                isError = true;
            }


            const methodHandle = getChildFromDotNotation(method);

            if (!isError && methodHandle === empty) {
                result = `beginInvokeAsync: Method '${method}' not defined`;
                isError = true;
            }

            if (!isError) {
                
                try {
                    const argsArray = serializer.deserialize(argsString);
                    result = await methodHandle(...argsArray);
                }
                catch (e) {
                    result = `${e}\nJS Stacktrace:${(e.stack || new Error().stack)}`;
                    isError = true;
                }
            }
            
            let resultString;
            if (isVoid && !isError) {
                resultString = null;
            } else {
                try {
                    resultString = serializer.serialize(result);
                } catch (e) {
                    result = `${e}\nJS Stacktrace:${(e.stack || new Error().stack)}`;
                    isError = true;
                }
            }
            
            try {
                endInvokeCallBack(invokeId, isError, resultString);
            } catch (e) {
                
                console.error(`BlazorWorker: beginInvokeAsync: Callback to ${initConf.endInvokeCallBackEndpoint} failed. Method: ${method}, args: ${argsString}`, e);
                throw e;
            }
        };*/

        // Import script from a path relative to approot
        self.importLocalScripts = (...urls) => {
            self.importScripts(urls.map(url => initConf.appRoot + (url.startsWith('/') ? '' : '/') + url));
        };

        self.isObjectDefined = (workerScopeObject) => {
            return getChildFromDotNotation(workerScopeObject) !== empty;
        };
    };

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = function (id, callbackInstance, initOptions) {
        let appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substr(0, appRoot.length - 1);
        }

        const initConf = {
            appRoot: appRoot,
            DependentAssemblyFilenames: initOptions.dependentAssemblyFilenames,
            deploy_prefix: initOptions.deployPrefix,
            MessageEndPoint: initOptions.messageEndPoint,
            InitEndPoint: initOptions.initEndPoint,
            endInvokeCallBackEndpoint: initOptions.endInvokeCallBackEndpoint,
            wasmRoot: initOptions.wasmRoot,
            blazorBoot: "_framework/blazor.boot.json",
            envMap: initOptions.envMap,
            debug: initOptions.debug
        };

        // Initialize worker
        const renderedConfig = JSON.stringify(initConf);
        const renderedInlineWorker = inlineWorker.replace('$initConf$', renderedConfig);
        window.URL = window.URL || window.webkitURL;
        const blob = new Blob([renderedInlineWorker], { type: 'application/javascript' });
        const workerUrl = URL.createObjectURL(blob);
        const worker = new Worker(workerUrl, { type: 'module'});
        workers[id] = {
            worker: worker,
            url: workerUrl
        };

        worker.onmessage = function (ev) {
            if (initOptions.debug) {
                console.debug(`BlazorWorker.js:worker[${id}]->blazor`, initOptions.callbackMethod, ev.data);
            }
            callbackInstance.invokeMethod(initOptions.callbackMethod, ev.data);
        };
    };

    const postMessage = function (workerId, message) {
        workers[workerId].worker.postMessage(message);
    };

    return {
        disposeWorker,
        initWorker,
        postMessage
    };
}();
