using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
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
                .AddLogger(ConsoleLogger.Default)
                .AddExporter(MarkdownExporter.GitHub);

            var switcher = new BenchmarkSwitcher(
                new[]
                {
                    typeof(AlternatingLeastSquaresBenchmark),
                });

            switcher.Run(args, config);
        }
    }
}
