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
                // replaces blob urls with appRoot urls. Mono will attempt to load dlls from self.location.href.
                
                args[0] = args[0].replace(blobRoot, initConf.appRoot);
                if (initConf.runtimePreprocessorSymbols.NET8_0_OR_GREATER) {
                    if (self.modifiedBlazorbootConfig && args[0].endsWith(initConf.blazorBoot)) {
                        return Promise.race([new Response(JSON.stringify(self.modifiedBlazorbootConfig),
                            { "status": 200, headers: { "Content-Type": "application/json" } })]);
                    }
                }
                if (args[0].endsWith("mono-config.json")) {
                    // TODO: Can this horrible hack be avoided by calling dotnet.withConfig ?
                    // https://github.com/dotnet/runtime/blob/main/src/mono/wasm/runtime/dotnet.d.ts#L75C5-L75C15
                    return Promise.race([new Response(JSON.stringify(self.blazorbootMonoConfig),
                        { "status": 200, headers: { "Content-Type": "application/json" } })]);
                }

                return target.apply(thisArg, args);
            }
        }
        self.nativeFetch = fetch;
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

        const onReady = () => {
            // Future messages goes directly to the message handler
            self.onmessage = msg => {
                messageHandler(msg.data);
            };

            try {
                self.initMethod();
            } catch (e) {
                console.error(`Init method ${JSON.stringify(initConf.initEndPoint)} failed`, e);
                throw e;
            }
        };

        // reduce dot notation to last member of chain
        const getChildFromDotNotation = (member, root) =>
            member.split(".").reduce((m, prop) => Object.hasOwnProperty.call(m, prop) ? m[prop] : empty, root || self);

        function patchBlazorBootConfig(blazorBootConfig) {
            blazorBootConfig.mainAssemblyName = "BlazorWorker.WorkerCore";

            for (const path of initConf.pruneBlazorBootConfig) {
                let parts = path.split('.');
                const lastPart = parts.splice(parts.length - 1)[0];

                let currentObject = blazorBootConfig;

                for (const part of parts) {
                    currentObject = currentObject[part];

                    if (currentObject === undefined) {
                        break;
                    }
                }

                if (!currentObject || !currentObject[lastPart]) {
                    console.warn(`Could not interpret the pruneBlazorBootConfig path ${path}`);
                }
                else {
                    delete currentObject[lastPart];
                }
            }
        }

        function getDotnetJsPath(dotnetJsFilename) {
            return `${initConf.appRoot}/${initConf.wasmRoot}/${dotnetJsFilename}`;
        }

        //TODO: This call could/should be session cached. But will the built-in blazor fetch service worker override
        // (PWA et al) do this already if configured ?
        let blazorBootPromise;
        if (initConf.runtimePreprocessorSymbols.NET7_0_OR_GREATER &&
            !initConf.runtimePreprocessorSymbols.NET10_0_OR_GREATER) {

            blazorBootPromise =
                fetch(`${initConf.appRoot}/${initConf.blazorBoot}`)
                .then(async response => {
                    const blazorboot = await response.json();
                    /* START NET8_0_OR_GREATER */
                    if (initConf.runtimePreprocessorSymbols.NET8_0_OR_GREATER) {
                        self.modifiedBlazorbootConfig = blazorboot;
                        self.modifiedBlazorbootConfig.mainAssemblyName = "BlazorWorker.WorkerCore";

                        if (initConf.pruneBlazorBootConfig) {
                            for (const path of initConf.pruneBlazorBootConfig) {
                                let parts = path.split('.');
                                const lastPart = parts.splice(parts.length - 1)[0];

                                let currentObject = self.modifiedBlazorbootConfig;

                                for (const part of parts) {
                                    currentObject = currentObject[part];

                                    if (currentObject === undefined) {
                                        break;
                                    }
                                }

                                if (!currentObject || !currentObject[lastPart]) {
                                    console.warn(`Could not interpret the pruneBlazorBootConfig path ${path}`);
                                }
                                else {
                                    delete currentObject[lastPart];
                                }
                            }
                        }
                    }
                    /* END NET8_0_OR_GREATER */
                    /* START NET7_0 */
                    if (initConf.runtimePreprocessorSymbols.NET7_0) {
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
                                    {
                                        "virtualPath": "runtimeconfig.bin",
                                        "behavior": "vfs",
                                        "name": "supportFiles/0_runtimeconfig.bin"
                                    },
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
                            "generatedFromBlazorBoot": true
                        };

                        self.blazorbootMonoConfig = blazorbootMonoConfig;
                    }
                    /* END NET7_0 */

                    let dotnetjsfilename = '';
                    /* START NET7_0 */
                    if (initConf.runtimePreprocessorSymbols.NET7_0) {
                        const runttimeSection = blazorboot.resources.runtime;
                        for (var p in runttimeSection) {
                            if (Object.prototype.hasOwnProperty.call(runttimeSection, p) && p.startsWith('dotnet.') && p.endsWith('.js')) {
                                dotnetjsfilename = p;
                            }
                        }

                        if (dotnetjsfilename === '') {
                            throw 'BlazorWorker: Unable to locate dotnetjs file in blazor boot config.';
                        }
                    }
                    /* END NET7_0 */
                    /* START NET8_0_OR_GREATER */
                    if (initConf.runtimePreprocessorSymbols.NET8_0_OR_GREATER) {
                        dotnetjsfilename = "dotnet.js";
                    }
                    /* END NET8_0_OR_GREATER */

                    return getDotnetJsPath(d);
                }, errorInfo => console.error("error loading blazorboot", errorInfo));
        }

        /* START NET10_0_OR_GREATER */
        if (initConf.runtimePreprocessorSymbols.NET10_0_OR_GREATER) {
            // NET10 no longer needs to load the boot config in advance (actually it no longer exists), and no modification is needed.
            // This may actually be the case for NET8 as well? Might be related to that we never call Run() since net8, so mainAssembly
            const dotnetJsPath = getDotnetJsPath('dotnet.js');

            if (!(initConf.pruneBlazorBootConfig)?.length) {
                // We dont need to patch anything here
                blazorBootPromise = Promise.resolve(dotnetJsPath);
            }
            else {
                blazorBootPromise = fetch(dotnetJsPath).then(async response => {
                    const dotNetJsContent = await response.text();

                    const blazorBootFromDotnetJsRegex = /\/\*json-start\*\/([\w\W]*?)\/\*json-end\*\//;
                    const match = dotNetJsContent.match(blazorBootFromDotnetJsRegex);

                    if (!match) {
                        throw new Error("Could not find embedded blazor boot config in dotnet.js");
                    }

                    let bootConfig = JSON.parse(match[1]);
                    patchBlazorBootConfig(bootConfig);

                    let patchedJs = dotNetJsContent.replace(
                        blazorBootFromDotnetJsRegex,
                        `/*json-start*/${JSON.stringify(bootConfig)}/*json-end*/`
                    );

                    // import.meta.url is used to get the scriptDirectory which shouldn't point to a blob
                    patchedJs = patchedJs.replace(/import\.meta\.url/, `'${dotnetJsPath}'`);

                    const blob = new Blob([patchedJs], { type: "application/javascript" });
                    const url = URL.createObjectURL(blob);

                    return url;
                });
            }
        }
        /* END NET10_0_OR_GREATER */

        if (!blazorBootPromise) {
            const errorMessage = 'BlazorWorker: Cannot determine boot method: Was likely unable to determine dotnet version.';
            console.error(errorMessage, { initConf })
            throw 'BlazorWorker: Cannot determine boot method: Was likely unable to determine dotnet version.';
        }

        blazorBootPromise.then(
            async (dotnetJsPath) => {
                const { dotnet } = await import(dotnetJsPath);

                const { setModuleImports, getAssemblyExports } = await dotnet
                    .withDiagnosticTracing(initConf.debug)
                    .withEnvironmentVariables(initConf.envMap)
                    .create();

                setModuleImports('BlazorWorker.js', {
                    PostMessage: (messagecontent) => {
                        self.postMessage(messagecontent);
                    },

                    PostMessageJsDirect: (messagecontent) => {
                        self.postMessage({ isJsDirect: true, jsData: messagecontent });
                    },

                    ImportLocalScripts: async (urls) => {
                        await self.importLocalScripts(urls);
                    },

                    IsObjectDefined: (workerScopeObject) => {
                        return self.isObjectDefined(workerScopeObject);
                    }
                });

                // Restore the fetch handler to the native fetch for non-invasive future fetch calls
                self.fetch = self.nativeFetch;

                self.BlazorWorker = {
                    getChildFromDotNotation,
                    getAssemblyExports,
                    setModuleImports,
                    empty
                };

                const getMethodFromMethodIdentifier = async function (methodIdentifier) {
                    const exports = await getAssemblyExports(methodIdentifier.assemblyName);
                    const method = getChildFromDotNotation(methodIdentifier.fullMethodName, exports);
                    if (!method || method === empty || !(method instanceof Function)) {
                        throw new Error(`Unable to find method '${methodIdentifier.fullMethodName}' in assembly '${methodIdentifier.assemblyName}}'. Are you missing a JSExport attribute?`);
                    }

                    return method;
                }

                messageHandler = await getMethodFromMethodIdentifier(initConf.messageEndPoint);
                self.initMethod = await getMethodFromMethodIdentifier(initConf.initEndPoint);

                onReady();

            });

        const empty = {};

        // Import module script from a path relative to approot
        self.importLocalScripts = async (urls) => {
            if (urls === undefined || urls === null) {
                return;
            }
            if (!urls.map) {
                urls = [urls]
            }
            for (const url of urls) {
                const urlToLoad = initConf.appRoot + (url.startsWith('/') ? '' : '/') + url;
                await import(urlToLoad);
            }
        };

        self.isObjectDefined = (workerScopeObject) => {
            return getChildFromDotNotation(workerScopeObject) !== empty;
        };
    };

    /* END workerDef */

    const inlineWorker = `self.onmessage = ${workerDef}()`; 

    const initWorker = function (id, callbackInstance, initOptions) {
        let appRoot = initOptions.appRoot;

        if (!appRoot) {
            appRoot = (document.getElementsByTagName('base')[0] || { href: window.location.origin }).href || "";
        }

        if (appRoot.endsWith("/")) {
            appRoot = appRoot.substring(0, appRoot.length - 1);
        }
        
        const initConf = {
            appRoot: appRoot,
            workerId: id,
            runtimePreprocessorSymbols: initOptions.runtimePreprocessorSymbols || {},
            messageEndPoint: initOptions.messageEndPoint,
            initEndPoint: initOptions.initEndPoint,
            endInvokeCallBackEndpoint: initOptions.endInvokeCallBackEndpoint,
            wasmRoot: initOptions.wasmRoot,
            blazorBoot: "_framework/blazor.boot.json",
            pruneBlazorBootConfig: initOptions.pruneBlazorBootConfig,
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

            if (ev.data.isJsDirect) {
                if (initOptions.debug) {
                    console.debug(`BlazorWorker.js:worker[${id}]->jsDirect`, initOptions.callbackMethod, ev.data.jsData);
                }

                var event = new CustomEvent("blazorworker:jsdirect",
                    { detail: { workerId: id, data: ev.data.jsData } }
                );
                window.dispatchEvent(event);

                return;
            }

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
