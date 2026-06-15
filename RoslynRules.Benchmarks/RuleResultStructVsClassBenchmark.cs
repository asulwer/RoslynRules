using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RoslynRules.Benchmarks
{
    /// <summary>
    /// In-process benchmark comparing struct vs class RuleResult performance.
    /// Addresses issue #48: RuleResult is a readonly struct stored in
    /// Dictionary and IReadOnlyList, which may cause boxing overhead.
    /// </summary>
    public static class RuleResultStructVsClassBenchmark
    {
        public static void Run()
        {
            Console.WriteLine("=== RuleResult Struct vs Class Benchmark ===\n");

            foreach (var count in new[] { 100, 1000, 10000 })
            {
                Console.WriteLine($"--- Child count: {count} ---");
                RunIteration(count);
                Console.WriteLine();
            }
        }

        private static void RunIteration(int childCount)
        {
            // Setup
            var structList = new List<StructRuleResult>(childCount);
            var classList = new List<ClassRuleResult>(childCount);
            var structDict = new Dictionary<Guid, StructRuleResult>(childCount);
            var classDict = new Dictionary<Guid, ClassRuleResult>(childCount);

            for (int i = 0; i < childCount; i++)
            {
                var id = Guid.NewGuid();
                structList.Add(new StructRuleResult(i % 2 == 0, id, $"Rule {i}", true));
                classList.Add(new ClassRuleResult(i % 2 == 0, id, $"Rule {i}", true));
                structDict[id] = structList[i];
                classDict[id] = classList[i];
            }

            int iterations = Math.Max(100000 / childCount, 100);

            // 1. List iteration
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                int c = 0;
                foreach (var item in structList)
                    if (item.Success) c++;
            }
            sw.Stop();
            var structListMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                int c = 0;
                foreach (var item in classList)
                    if (item.Success) c++;
            }
            sw.Stop();
            var classListMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"  List iteration:    Struct {structListMs:F2}ms / Class {classListMs:F2}ms (ratio: {classListMs / structListMs:F2}x)");

            // 2. Dictionary lookup
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                int c = 0;
                foreach (var key in structDict.Keys)
                    if (structDict[key].Success) c++;
            }
            sw.Stop();
            var structDictMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                int c = 0;
                foreach (var key in classDict.Keys)
                    if (classDict[key].Success) c++;
            }
            sw.Stop();
            var classDictMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"  Dictionary lookup: Struct {structDictMs:F2}ms / Class {classDictMs:F2}ms (ratio: {classDictMs / structDictMs:F2}x)");

            // 3. IReadOnlyList access (boxing concern)
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                IReadOnlyList<StructRuleResult> ro = structList;
                int c = 0;
                for (int j = 0; j < ro.Count; j++)
                    if (ro[j].Success) c++;
            }
            sw.Stop();
            var structRoMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                IReadOnlyList<ClassRuleResult> ro = classList;
                int c = 0;
                for (int j = 0; j < ro.Count; j++)
                    if (ro[j].Success) c++;
            }
            sw.Stop();
            var classRoMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"  IReadOnlyList:     Struct {structRoMs:F2}ms / Class {classRoMs:F2}ms (ratio: {classRoMs / structRoMs:F2}x)");

            // 4. LINQ Where
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                _ = structList.Where(r => r.Success).Count();
            }
            sw.Stop();
            var structLinqMs = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                _ = classList.Where(r => r.Success).Count();
            }
            sw.Stop();
            var classLinqMs = sw.Elapsed.TotalMilliseconds;

            Console.WriteLine($"  LINQ Where:        Struct {structLinqMs:F2}ms / Class {classLinqMs:F2}ms (ratio: {classLinqMs / structLinqMs:F2}x)");

            // Memory: GC pressure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long memBefore = GC.GetTotalMemory(true);

            var _structCopy = new List<StructRuleResult>(structList); // copy
            long memAfterStruct = GC.GetTotalMemory(true);
            GC.Collect();

            var _classCopy = new List<ClassRuleResult>(classList); // copy (references only)
            long memAfterClass = GC.GetTotalMemory(true);
            GC.Collect();

            Console.WriteLine($"  Memory (List copy): Struct {(memAfterStruct - memBefore):N0} bytes / Class {(memAfterClass - memAfterStruct):N0} bytes");
        }

        // ─── Struct mirror of RuleResult (core fields) ───
        public readonly struct StructRuleResult
        {
            public bool Success { get; }
            public Guid RuleId { get; }
            public string RuleDescription { get; }
            public bool IsActive { get; }

            public StructRuleResult(bool success, Guid ruleId, string ruleDescription, bool isActive)
            {
                Success = success;
                RuleId = ruleId;
                RuleDescription = ruleDescription;
                IsActive = isActive;
            }
        }

        // ─── Class equivalent ───
        public class ClassRuleResult
        {
            public bool Success { get; }
            public Guid RuleId { get; }
            public string RuleDescription { get; }
            public bool IsActive { get; }

            public ClassRuleResult(bool success, Guid ruleId, string ruleDescription, bool isActive)
            {
                Success = success;
                RuleId = ruleId;
                RuleDescription = ruleDescription;
                IsActive = isActive;
            }
        }
    }
}
