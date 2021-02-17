using Microsoft.JSInterop;
using System;
using System.Text.Json;

namespace BlazorWorker.Extensions.JSRuntime
{
    public class DotNetObjectReferenceTracker
    {
        public static JsonEncodedText DotNetObjectRefKey = JsonEncodedText.Encode("__dotNetObject");

        internal static DotNetObjectReference<T> GetObjectReference<T>(long dotNetObjectId) where T : class
        {
            throw new NotImplementedException();
        }

        internal static long TrackObjectReference<T>(DotNetObjectReference<T> value) where T: class
        {
            throw new NotImplementedException();
        }
    }
}
