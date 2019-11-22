using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorker.Demo.Shared
{
    public class FibonacciService
    {
        public long Fibonacci(long forValue)
        {
            var result = 1;
            for (int i = 1; i <= forValue; i++)
            {
                result *= i;
            }

            return result;
        }
    }
}
