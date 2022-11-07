using System;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using MathNet.Numerics;

namespace Implicit.Benchmark
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(Control.Describe());

            var config = ManualConfig.CreateEmpty()
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddLogger(ConsoleLogger.Default);

            var switcher = new BenchmarkSwitcher(
                new[]
                {
                    typeof(AlternatingLeastSquaresBenchmark),
                });

            switcher.Run(args, config);
        }
    }
}
