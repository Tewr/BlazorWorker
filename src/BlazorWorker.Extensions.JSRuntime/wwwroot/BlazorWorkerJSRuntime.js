class DotNetObjectProxy {
    constructor(id) {
        this.__dotNetObject = id;
        this.serializer = self.jsRuntimeSerializers.get('BlazorWorkerJSRuntimeSerializer');
    }

    invokeMethodAsync(methodName, ...methodArgs) {
        return new Promise((resolve, reject) => {
            try {
                const argsString = this.serializer.serialize({
                    method,
                    methodargs: methodArgs || []
                });
                var result = self.Module.mono_call_static_method("[BlazorWorker.Extensions.JSRuntime]BlazorWorker.Extensions.JSRuntime.BlazorWorkerJSRuntime:InvokeMethod", this.__dotNetObject, argsString);
                resolve(result);
            } catch (e) {
                reject(e);
            }
        });
    }
}

class BlazorWorkerJSRuntimeSerializer  {
    
    constructor() {
        this.baseSerializer = self.jsRuntimeSerializers.get('nativejson');
        this.dotNetObjectRefKey = "__dotNetObject";
    }

    serialize(o) {
        return this.baseSerializer.serialize(o);
    }

    deserialize(s) {
        let o = this.baseSerializer.deserialize(s);
        recurseObjectProperties(o, (obj, property) => {
            if (property === this.dotNetObjectRefKey) {
                obj = new DotNetObjectProxy(obj[property]);
            }
        });
        return o;
    }

    recurseObjectProperties(obj, transformer) {
        Object.keys(obj).forEach(property => {
            if (obj[property] !== null && typeof obj[property] === "object") {
                recurseObjectProperties(obj[property], transformer);
            } else {
                transformer(obj, property);
            }
        });
    }
};


self.jsRuntimeSerializers.set('BlazorWorkerJSRuntimeSerializer', new BlazorWorkerJSRuntimeSerializer());