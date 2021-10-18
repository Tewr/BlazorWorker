using BlazorWorker.WorkerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWorker.Demo.Shared
{
    public class PiProgress
    {
        public int Progress { get; set; }
    }


    /// <summary>
    /// This service runs insinde the worker.
    /// </summary>
    public class MathsService
    {
        public event EventHandler<PiProgress> Pi;

        private IEnumerable<int> AlternatingSequence(int start = 0)
        {
            int i;
            bool flip;
            if (start == 0)
            {
                yield return 1;
                i = 1;
                flip = false;
            }
            else
            {
                i = (start * 2) - 1;
                flip = start % 2 == 0;
            }

            while (true) yield return ((flip = !flip) ? -1 : 1) * (i += 2);
        }

        public async Task<double> EstimatePI(int sumLength)
        {
            TheArrayExperiment();
            var lastReport = 0;
            await Task.Delay(100);
            return (4 * AlternatingSequence().Take(sumLength)
                .Select((x, i) => {
                    // Keep reporting events down a bit, serialization is expensive!
                    var progressDelta = (Math.Abs(i - lastReport) / (double)sumLength) * 100;
                    if (progressDelta > 3 || i >= sumLength - 1)
                    {
                        lastReport = i;
                        Pi?.Invoke(this, new PiProgress() { Progress = i });
                    }
                    return x; })
                .Sum(x => 1.0 / x));
        }

        public void TheArrayExperiment()
        {
            //var assembly = Assembly.Load("System.Private.Runtime.InteropServices.JavaScript");
            ////JSInvokeService.Invoke<object>("tempByteArray", new PostMessageArg { ByteArray = new byte[] { 4, 5, 6 } });
            //var arrayType = assembly.GetType("System.Runtime.InteropServices.JavaScript.Array");
            //if (arrayType is null)
            //{
            //    throw new Exception("Cannot load System.Runtime.InteropServices.JavaScript.Array");
            //}
            //var array = Activator.CreateInstance(arrayType, new object[] { 4, 5, 6 });
            var p = new List<byte>();
            for (int i = 0; i < 800; i++)
            {
                p.Add((byte)(i % 254));
            }
            var source = p.ToArray().AsSpan();
            //var type = Type.GetType("System.Runtime.InteropServices.JavaScript.Uint32Array");
            //var uint8array = Activator.CreateInstance(type);
            //type.GetMethod("CopyFrom").Invoke(uint8array, new[] { source });
            var uint8array = JSInvokeService.Invoke<object>("returnTempByteArray");
            Console.WriteLine("returnTempByteArray result is of type" + uint8array.GetType().FullName);
            var jsHandle = (int)uint8array.GetType().GetProperty("JSHandle").GetValue(uint8array);
            Console.WriteLine($"uint8array handle is {jsHandle}");
            JSInvokeService.TypedArrayCopyFrom(jsHandle, source);
            JSInvokeService.Invoke<object>("tempByteArray");
            //JSInvokeService.Invoke<object>("tempByteArray", (ulong)123);
        }



        [StructLayout(LayoutKind.Explicit)]
        public struct PostMessageArg
        {
            [FieldOffset(0)]
            public long Identifier;
            [FieldOffset(8)]
            public string Message;
            [FieldOffset(12)]
            public byte[] ByteArray;
        }

        public double EstimatePISlice(int sumStart, int sumLength)
        {
            Console.WriteLine($"EstimatePISlice({sumStart},{sumLength})");
            var lastReport = 0;
            return AlternatingSequence(sumStart)
                .Take(sumLength)
                .Select((x, i) => {

                    // Keep reporting events down a bit, serialization is expensive!
                    var progressDelta = (Math.Abs(i - lastReport) / (double)sumLength) * 100;
                    if (progressDelta > 3 || i >= sumLength - 1)
                    {
                        lastReport = i;
                        Pi?.Invoke(this, new PiProgress() { Progress = i });
                    }
                    return x;
                })
                .Sum(x => 1.0 / x);

        }
    }
}
