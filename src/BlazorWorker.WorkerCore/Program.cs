﻿using System;
#if NET7_0_OR_GREATER

Console.WriteLine("Hello, Dotnet Worker in Browser!");

#else

namespace BlazorWorker.WorkerCore
{
    public class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello, Dotnet Worker in Browser!");
        }
    }
}
#endif