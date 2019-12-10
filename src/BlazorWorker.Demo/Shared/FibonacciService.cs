using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.Demo.Shared
{
    public class FibonacciService
    {
        public event EventHandler<long> Fibbo;

        public long Fibonacci(long forValue)
        {
            if (forValue == 0L)
            {
                Fibbo?.Invoke(this, forValue);
                return forValue;
            }

            var last = 0L;
            var sum = 1L;
            for (var i = 0L; i < forValue - 1L; i++)
            {
                var curr = last;
                last = sum;
                sum += curr;
                Fibbo?.Invoke(this, curr);
            }

            return sum;
        }
    }
}
