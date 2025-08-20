using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using System.Reflection;

namespace MCGame.Tests.Benchmark
{
    /// <summary>
    /// 基准测试运行器
    /// 运行所有性能基准测试
    /// </summary>
    public class BenchmarkRunner
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== MCGame ECS 基准测试 ===");
            Console.WriteLine("开始运行性能基准测试...");
            Console.WriteLine();

            // 配置基准测试
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun.WithWarmupCount(3).WithIterationCount(5))
                .AddExporter(CsvExporter.Default)
                .AddExporter(MarkdownExporter.GitHub)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .WithOptions(ConfigOptions.KeepBenchmarkFiles)
                .WithOptions(ConfigOptions.StopOnFirstError);

            try
            {
                // 运行基准测试
                var summary = BenchmarkRunner.Run<ECSBenchmarks>(config);

                Console.WriteLine();
                Console.WriteLine("=== 基准测试完成 ===");
                Console.WriteLine($"测试结果已保存到: {summary.ResultsDirectoryPath}");
                Console.WriteLine();
                Console.WriteLine("主要性能指标:");
                
                foreach (var report in summary.Reports)
                {
                    Console.WriteLine($"{report.BenchmarkCase.DisplayInfo}:");
                    Console.WriteLine($"  平均执行时间: {report.ResultStatistics.Mean:F2} ms");
                    Console.WriteLine($"  标准差: {report.ResultStatistics.StandardDeviation:F2} ms");
                    Console.WriteLine($"  内存分配: {report.GcStats.BytesAllocatedPerOperation} bytes");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"基准测试运行失败: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        /// <summary>
        /// 运行快速基准测试
        /// </summary>
        public static void RunQuickBenchmark()
        {
            Console.WriteLine("=== MCGame ECS 快速基准测试 ===");
            Console.WriteLine("开始运行快速性能基准测试...");
            Console.WriteLine();

            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun.WithWarmupCount(1).WithIterationCount(3))
                .AddExporter(CsvExporter.Default)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            try
            {
                var summary = BenchmarkRunner.Run<ECSBenchmarks>(config);

                Console.WriteLine();
                Console.WriteLine("=== 快速基准测试完成 ===");
                Console.WriteLine("主要性能指标:");
                
                foreach (var report in summary.Reports)
                {
                    Console.WriteLine($"{report.BenchmarkCase.DisplayInfo}:");
                    Console.WriteLine($"  平均执行时间: {report.ResultStatistics.Mean:F2} ms");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"快速基准测试运行失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 运行特定基准测试
        /// </summary>
        public static void RunSpecificBenchmark(string benchmarkName)
        {
            Console.WriteLine($"=== MCGame ECS 特定基准测试: {benchmarkName} ===");
            Console.WriteLine();

            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddJob(Job.ShortRun.WithWarmupCount(2).WithIterationCount(3))
                .AddFilter(benchmarkName);

            try
            {
                var summary = BenchmarkRunner.Run<ECSBenchmarks>(config);

                Console.WriteLine();
                Console.WriteLine($"=== 特定基准测试完成: {benchmarkName} ===");
                
                foreach (var report in summary.Reports)
                {
                    Console.WriteLine($"{report.BenchmarkCase.DisplayInfo}:");
                    Console.WriteLine($"  平均执行时间: {report.ResultStatistics.Mean:F2} ms");
                    Console.WriteLine($"  标准差: {report.ResultStatistics.StandardDeviation:F2} ms");
                    Console.WriteLine($"  内存分配: {report.GcStats.BytesAllocatedPerOperation} bytes");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"特定基准测试运行失败: {ex.Message}");
            }
        }
    }
}