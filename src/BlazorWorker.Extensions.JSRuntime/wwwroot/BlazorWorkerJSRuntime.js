class DotNetObjectProxy {
    constructor(id) {
        this.__dotNetObject = id;
        this.serializer = self.jsRuntimeSerializers.get('BlazorWorkerJSRuntimeSerializer');
    }

    invokeMethodAsync(methodName, ...methodArgs) {
        return new Promise((resolve, reject) => {
            try {
                const argsString = this.serializer.serialize({
                    methodName,
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
    }

    serialize = (o) => this.baseSerializer.serialize(o);
    

    deserialize = (s) => {
        let deserializedObj = this.baseSerializer.deserialize(s);
        deserializedObj = BlazorWorkerJSRuntimeSerializer.recursivelyFindDotnetObjectProxy(deserializedObj);
        return deserializedObj;
    }

    static recursivelyFindDotnetObjectProxy(obj) {
        
        const recursion = BlazorWorkerJSRuntimeSerializer.recursivelyFindDotnetObjectProxy;
        const dotnetObjectKey = "__dotNetObject";
        const keys = Object.keys(obj);
        if (keys.length === 1 && keys[0] === dotnetObjectKey) {
            return new DotNetObjectProxy(obj[dotnetObjectKey]);
        }

        for (let i = 0; i < keys.length; i++) {
            const property = keys[i];
            let value = obj[property];
            if (value !== null && typeof value === "object") {
                obj[property] = recursion(value);
            }
        }

        return obj;
    }
};

self.jsRuntimeSerializers.set('BlazorWorkerJSRuntimeSerializer', new BlazorWorkerJSRuntimeSerializer());