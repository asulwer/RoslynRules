using FluentAssertions;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Execution
{
    public class RuleStreamingTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleStreamingTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        // ==================== CancellationToken on ExecuteAsync ====================

        [Fact]
        public async Task ExecuteAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var workflow = new Workflow
            {
                Description = "Cancel test",
                Rules = new List<Rule>
                {
                    new Rule { Description = "R1", Expression = "customer.Age >= 0", IsActive = true },
                    new Rule { Description = "R2", Expression = "customer.Age >= 0", IsActive = true },
                    new Rule { Description = "R3", Expression = "customer.Age >= 0", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () =>
            {
                await foreach (var _ in workflow.ExecuteAsync(_parameters, cts.Token))
                {
                    // Should never get here
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ExecuteAsync_CancelMidStream_StopsEarly()
        {
            var workflow = new Workflow
            {
                Description = "Mid-cancel",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "R1",
                        Expression = "customer.Age >= 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "R2",
                        Expression = "customer.Age >= 0",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "R3",
                        Expression = "customer.Age >= 0",
                        IsActive = true
                    }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var cts = new CancellationTokenSource();
            var count = 0;

            try
            {
                await foreach (var result in workflow.ExecuteAsync(_parameters, cts.Token))
                {
                    count++;
                    if (count == 1)
                        cts.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            count.Should().BeLessThan(3);
        }

        // ==================== CancellationToken on ExecuteParallelAsync ====================

        [Fact]
        public async Task ExecuteParallelAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var workflow = new Workflow
            {
                Description = "Parallel cancel",
                Rules = new List<Rule>
                {
                    new Rule { Description = "R1", Expression = "customer.Age >= 0", IsActive = true },
                    new Rule { Description = "R2", Expression = "customer.Age >= 0", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately before execution

            var act = () => workflow.ExecuteParallelAsync(_parameters, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        // ==================== ExecuteBufferedAsync ====================

        [Fact]
        public async Task ExecuteBufferedAsync_DefaultBufferSize_YieldsChunks()
        {
            var workflow = new Workflow
            {
                Description = "Buffered",
                Rules = new List<Rule>
                {
                    new Rule { Description = "R1", Expression = "true", IsActive = true },
                    new Rule { Description = "R2", Expression = "true", IsActive = true },
                    new Rule { Description = "R3", Expression = "true", IsActive = true },
                    new Rule { Description = "R4", Expression = "true", IsActive = true },
                    new Rule { Description = "R5", Expression = "true", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var chunks = new List<RuleResult[]>();
            await foreach (var chunk in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 2, cancellationToken: TestContext.Current.CancellationToken))
            {
                chunks.Add(chunk);
            }

            chunks.Should().HaveCount(3); // 2, 2, 1
            chunks[0].Should().HaveCount(2);
            chunks[1].Should().HaveCount(2);
            chunks[2].Should().HaveCount(1);
        }

        [Fact]
        public async Task ExecuteBufferedAsync_DependentRule_NotInSameChunkAsDependency()
        {
            var depId = Guid.NewGuid();
            var dependentId = Guid.NewGuid();

            var workflow = new Workflow
            {
                Description = "Buffered with dependency",
                Rules = new List<Rule>
                {
                    new Rule(depId) { Description = "Dependency", Expression = "true", IsActive = true },
                    new Rule { Description = "Independent", Expression = "true", IsActive = true },
                    new Rule(dependentId)
                    {
                        Description = "Dependent",
                        Expression = "true",
                        IsActive = true,
                        DependsOnRuleId = depId
                    }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var chunks = new List<RuleResult[]>();
            // Buffer large enough to hold every rule in one chunk if dependencies were ignored.
            await foreach (var chunk in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 10, cancellationToken: TestContext.Current.CancellationToken))
            {
                chunks.Add(chunk);
            }

            int ChunkOf(Guid id) => chunks.FindIndex(c => c.Any(r => r.RuleId == id));

            var depChunk = ChunkOf(depId);
            var dependentChunk = ChunkOf(dependentId);

            depChunk.Should().BeGreaterThanOrEqualTo(0);
            dependentChunk.Should().BeGreaterThan(depChunk,
                "a dependent rule must never share a buffer chunk with (or precede) its dependency");
        }

        [Fact]
        public async Task ExecuteBufferedAsync_BufferSizeZero_Throws()
        {
            var workflow = new Workflow
            {
                Description = "Bad buffer size",
                Rules = new List<Rule> { new Rule { Description = "R1", Expression = "true", IsActive = true } }
            };
            workflow.Compile(_parameters);

            var act = async () =>
            {
                await foreach (var _ in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 0, cancellationToken: TestContext.Current.CancellationToken))
                {
                }
            };

            await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        }

        [Fact]
        public async Task ExecuteBufferedAsync_BufferSizeEqualsRuleCount_SingleChunk()
        {
            var workflow = new Workflow
            {
                Description = "Single chunk",
                Rules = new List<Rule>
                {
                    new Rule { Description = "R1", Expression = "true", IsActive = true },
                    new Rule { Description = "R2", Expression = "true", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var chunks = new List<RuleResult[]>();
            await foreach (var chunk in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 10, cancellationToken: TestContext.Current.CancellationToken))
            {
                chunks.Add(chunk);
            }

            chunks.Should().HaveCount(1);
            chunks[0].Should().HaveCount(2);
        }

        [Fact]
        public async Task ExecuteBufferedAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var workflow = new Workflow
            {
                Description = "Buffered cancel",
                Rules = new List<Rule>
                {
                    new Rule { Description = "R1", Expression = "true", IsActive = true },
                    new Rule { Description = "R2", Expression = "true", IsActive = true },
                    new Rule { Description = "R3", Expression = "true", IsActive = true }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () =>
            {
                await foreach (var _ in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 1, cancellationToken: cts.Token))
                {
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task ExecuteBufferedAsync_RespectsPriority()
        {
            var workflow = new Workflow
            {
                Description = "Priority buffered",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Low", Expression = "true", IsActive = true, Priority = 0 },
                    new Rule { Description = "High", Expression = "true", IsActive = true, Priority = 10 }
                }
            };

            workflow.Validate();
            workflow.Compile(_parameters);

            var chunks = new List<RuleResult[]>();
            await foreach (var chunk in workflow.ExecuteBufferedAsync(_parameters, bufferSize: 1, cancellationToken: TestContext.Current.CancellationToken))
            {
                chunks.Add(chunk);
            }

            chunks[0][0].RuleDescription.Should().Be("High");
            chunks[1][0].RuleDescription.Should().Be("Low");
        }

        // ==================== RuleBatch IAsyncEnumerable ====================

        [Fact]
        public async Task RuleBatch_EvaluateAsync_StreamsResults()
        {
            var batch = new Batch.RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "customer.Age >= 18", IsActive = true })
                .AddRule(new Rule { Description = "R2", Expression = "customer.Age >= 0", IsActive = true });

            batch.Compile(_parameters);

            var results = new List<RuleResult>();
            await foreach (var result in batch.EvaluateAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public async Task RuleBatch_EvaluateAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var batch = new Batch.RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "true", IsActive = true })
                .AddRule(new Rule { Description = "R2", Expression = "true", IsActive = true });

            batch.Compile(_parameters);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var act = async () =>
            {
                await foreach (var _ in batch.EvaluateAsync(_parameters, cts.Token))
                {
                }
            };

            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task RuleBatch_EvaluateParallelAsync_WithCancellation_ThrowsOperationCanceledException()
        {
            var batch = new Batch.RuleBatch()
                .AddRule(new Rule { Description = "R1", Expression = "customer.Age >= 0", IsActive = true })
                .AddRule(new Rule { Description = "R2", Expression = "customer.Age >= 0", IsActive = true });

            batch.Compile(_parameters);

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately before execution

            var act = () => batch.EvaluateParallelAsync(_parameters, cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();
        }

        // ==================== Inactive Workflow ====================

        [Fact]
        public async Task ExecuteAsync_InactiveWorkflow_YieldsNothing()
        {
            var workflow = new Workflow
            {
                Description = "Inactive",
                IsActive = false,
                Rules = new List<Rule> { new Rule { Description = "R1", Expression = "true", IsActive = true } }
            };

            var results = new List<RuleResult>();
            await foreach (var result in workflow.ExecuteAsync(_parameters, TestContext.Current.CancellationToken))
            {
                results.Add(result);
            }

            results.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteBufferedAsync_InactiveWorkflow_YieldsNothing()
        {
            var workflow = new Workflow
            {
                Description = "Inactive",
                IsActive = false,
                Rules = new List<Rule> { new Rule { Description = "R1", Expression = "true", IsActive = true } }
            };

            var results = new List<RuleResult[]>();
            await foreach (var chunk in workflow.ExecuteBufferedAsync(_parameters, cancellationToken: TestContext.Current.CancellationToken))
            {
                results.Add(chunk);
            }

            results.Should().BeEmpty();
        }
    }
}