using BenchmarkDotNet.Running;
using RoslynRules.Benchmarks;

// Run BenchmarkDotNet benchmarks
BenchmarkRunner.Run<CompilerPipelineDotNetBenchmark>();

// Run existing manual benchmarks
Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine("  Manual Benchmarks");
Console.WriteLine("========================================");
Console.WriteLine();

RuleResultStructVsClassBenchmark.Run();
Console.WriteLine();

CompilerPipelineBenchmark.Run();
Console.WriteLine();

RuleExecutionBenchmark.Run();
Console.WriteLine();

WorkflowBenchmark.Run();
Console.WriteLine();

CacheBenchmark.Run();
Console.WriteLine();

RuleTemplateBenchmark.Run();
Console.WriteLine();

JsonSerializationBenchmark.Run();
Console.WriteLine();

MemoryPressureBenchmark.Run();
Console.WriteLine();

Console.WriteLine("========================================");
Console.WriteLine("  All benchmarks completed");
Console.WriteLine("========================================");
