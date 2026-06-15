using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore;
using RoslynRules.EntityFrameworkCore.Entities;
using RoslynRules.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RoslynRules.Tests.Extensions
{
    /// <summary>
    /// Tests for EntityFrameworkCore extension.
    /// Uses InMemory database to avoid external DB dependencies.
    /// </summary>
    public class EntityFrameworkCoreTests : IDisposable
    {
        private readonly TestDbContext _db;

        public EntityFrameworkCoreTests()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TestDbContext(options);
            _db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        // ==================== Entity Mapping ====================

        [Fact]
        public void WorkflowEntity_MapsToDatabase()
        {
            var workflow = new WorkflowEntity
            {
                Description = "Test workflow",
                Version = "2.1.0",
                ModifiedBy = "test-user",
                CreatedAt = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc),
                ModifiedAt = new DateTime(2024, 6, 1, 14, 0, 0, DateTimeKind.Utc)
            };

            _db.Workflows.Add(workflow);
            _db.SaveChanges();

            var loaded = _db.Workflows.Find(workflow.Id);
            loaded.Should().NotBeNull();
            loaded!.Description.Should().Be("Test workflow");
            loaded.Version.Should().Be("2.1.0");
            loaded.ModifiedBy.Should().Be("test-user");
            loaded.CreatedAt.Should().Be(new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc));
            loaded.ModifiedAt.Should().Be(new DateTime(2024, 6, 1, 14, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void RuleEntity_MapsToDatabase()
        {
            var rule = new RuleEntity
            {
                Description = "Test rule",
                Expression = "x > 0",
                Action = "result = x",
                Priority = 10,
                CacheDuration = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromSeconds(30),
                Version = "1.5.3",
                DescriptionKey = "rule.test",
                ModifiedBy = "rule-author"
            };

            _db.Rules.Add(rule);
            _db.SaveChanges();

            var loaded = _db.Rules.Find(rule.Id);
            loaded.Should().NotBeNull();
            loaded!.Description.Should().Be("Test rule");
            loaded.Expression.Should().Be("x > 0");
            loaded.Action.Should().Be("result = x");
            loaded.Priority.Should().Be(10);
            loaded.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
            loaded.Timeout.Should().Be(TimeSpan.FromSeconds(30));
            loaded.Version.Should().Be("1.5.3");
            loaded.DescriptionKey.Should().Be("rule.test");
            loaded.ModifiedBy.Should().Be("rule-author");
        }

        // ==================== ToDomainModel Conversion ====================

        [Fact]
        public void RuleEntity_ToDomainModel_PreservesAllProperties()
        {
            var entity = new RuleEntity
            {
                Id = Guid.NewGuid(),
                Description = "Full rule",
                Expression = "x > 0",
                Action = "result = x",
                Priority = 10,
                IsActive = true,
                CacheDuration = TimeSpan.FromMinutes(5),
                Timeout = TimeSpan.FromSeconds(30),
                DependsOnRuleId = Guid.NewGuid(),
                ParentRuleId = Guid.NewGuid(),
                WorkflowId = Guid.NewGuid(),
                Version = "2.1.3-alpha+build.123",
                DescriptionKey = "rule.full",
                ModifiedBy = "author",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var domain = entity.ToDomainModel();

            domain.Id.Should().Be(entity.Id);
            domain.Description.Should().Be("Full rule");
            domain.Expression.Should().Be("x > 0");
            domain.Action.Should().Be("result = x");
            domain.Priority.Should().Be(10);
            domain.IsActive.Should().BeTrue();
            domain.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
            domain.Timeout.Should().Be(TimeSpan.FromSeconds(30));
            domain.DependsOnRuleId.Should().Be(entity.DependsOnRuleId);
            domain.ParentRuleId.Should().Be(entity.ParentRuleId);
            domain.WorkflowId.Should().Be(entity.WorkflowId);
            domain.Version.Should().Be(new RuleVersion(2, 1, 3, "alpha", "build.123"));
            domain.DescriptionKey.Should().Be("rule.full");
            domain.ModifiedBy.Should().Be("author");
            domain.CreatedAt.Should().Be(entity.CreatedAt);
            domain.ModifiedAt.Should().Be(entity.ModifiedAt);
        }

        [Fact]
        public void WorkflowEntity_ToDomainModel_PreservesAllProperties()
        {
            var entity = new WorkflowEntity
            {
                Id = Guid.NewGuid(),
                Description = "Full workflow",
                IsActive = true,
                Version = "3.2.1",
                ModifiedBy = "workflow-author",
                CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                ModifiedAt = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var domain = entity.ToDomainModel();

            domain.Id.Should().Be(entity.Id);
            domain.Description.Should().Be("Full workflow");
            domain.IsActive.Should().BeTrue();
            domain.Version.Should().Be(new RuleVersion(3, 2, 1));
            domain.ModifiedBy.Should().Be("workflow-author");
            domain.CreatedAt.Should().Be(entity.CreatedAt);
            domain.ModifiedAt.Should().Be(entity.ModifiedAt);
        }

        [Fact]
        public void RuleEntity_ToDomainModel_PreservesChildRules()
        {
            var parent = new RuleEntity
            {
                Description = "Parent",
                Expression = "true"
            };

            parent.ChildRules.Add(new RuleEntity
            {
                Description = "Child1",
                Expression = "x > 0"
            });
            parent.ChildRules.Add(new RuleEntity
            {
                Description = "Child2",
                Expression = "false",
                Priority = 5
            });

            var domain = parent.ToDomainModel();

            domain.ChildRules.Should().HaveCount(2);
            domain.ChildRules[0].Description.Should().Be("Child1");
            domain.ChildRules[1].Description.Should().Be("Child2");
            domain.ChildRules[1].Priority.Should().Be(5);
        }

        [Fact]
        public void WorkflowEntity_ToDomainModel_OnlyIncludesTopLevelRules()
        {
            var workflow = new WorkflowEntity
            {
                Description = "With children"
            };

            var parent = new RuleEntity
            {
                Description = "Parent",
                Expression = "true"
            };
            var child = new RuleEntity
            {
                Description = "Child",
                Expression = "x > 0",
                ParentRuleId = parent.Id,
                ParentRule = parent
            };
            parent.ChildRules.Add(child);
            workflow.Rules.Add(parent);

            var domain = workflow.ToDomainModel();

            domain.Rules.Should().HaveCount(1);
            domain.Rules[0].Description.Should().Be("Parent");
            domain.Rules[0].ChildRules.Should().HaveCount(1);
            domain.Rules[0].ChildRules[0].Description.Should().Be("Child");
        }

        // ==================== Cascade Delete ====================

        [Fact]
        public void WorkflowEntity_Delete_CascadesToRules()
        {
            var workflow = new WorkflowEntity
            {
                Description = "Cascade test"
            };
            workflow.Rules.Add(new RuleEntity
            {
                Description = "R1",
                Expression = "true",
                Workflow = workflow,
                WorkflowId = workflow.Id
            });

            _db.Workflows.Add(workflow);
            _db.SaveChanges();

            var workflowId = workflow.Id;
            var ruleCountBefore = _db.Rules.Count(r => r.WorkflowId == workflowId);
            ruleCountBefore.Should().Be(1);

            _db.Workflows.Remove(workflow);
            _db.SaveChanges();

            var ruleCountAfter = _db.Rules.Count(r => r.WorkflowId == workflowId);
            ruleCountAfter.Should().Be(0);
        }

        [Fact]
        public void RuleEntity_Delete_CascadesToChildRules()
        {
            var parent = new RuleEntity
            {
                Description = "Parent"
            };
            var child = new RuleEntity
            {
                Description = "Child",
                ParentRule = parent,
                ParentRuleId = parent.Id
            };
            parent.ChildRules.Add(child);

            _db.Rules.Add(parent);
            _db.SaveChanges();

            var parentId = parent.Id;
            var childCountBefore = _db.Rules.Count(r => r.ParentRuleId == parentId);
            childCountBefore.Should().Be(1);

            _db.Rules.Remove(parent);
            _db.SaveChanges();

            var childCountAfter = _db.Rules.Count(r => r.ParentRuleId == parentId);
            childCountAfter.Should().Be(0);
        }

        // ==================== End-to-End: Save, Load, Convert, Execute ====================

        [Fact]
        public void EndToEnd_SaveEntity_LoadEntity_ConvertToDomain_Execute()
        {
            var workflow = new WorkflowEntity
            {
                Description = "E2E test",
                Rules =
                {
                    new RuleEntity
                    {
                        Description = "R1",
                        Expression = "x > 0"
                    }
                }
            };

            _db.Workflows.Add(workflow);
            _db.SaveChanges();

            var loaded = _db.Workflows.Find(workflow.Id);
            var domain = loaded!.ToDomainModel();
            domain.Compile(new[] { new RuleParameter("x", typeof(int)) });

            var results = domain.Execute(new[] { new RuleParameter("x", typeof(int), 5) }).ToArray();

            results.Should().HaveCount(1);
            results[0].Success.Should().BeTrue();
        }

        [Fact]
        public void EndToEnd_VersionedEntity_RoundTrip()
        {
            var workflow = new WorkflowEntity
            {
                Description = "Versioned E2E",
                Version = "2.0.0",
                Rules =
                {
                    new RuleEntity
                    {
                        Description = "R1",
                        Expression = "true",
                        Version = "1.1.0"
                    }
                }
            };

            _db.Workflows.Add(workflow);
            _db.SaveChanges();

            var loaded = _db.Workflows.Find(workflow.Id);
            var domain = loaded!.ToDomainModel();

            domain.Version.Should().Be(new RuleVersion(2, 0, 0));
            domain.Rules[0].Version.Should().Be(new RuleVersion(1, 1, 0));
        }

        [Fact]
        public void EndToEnd_TimestampedEntity_RoundTrip()
        {
            var created = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
            var modified = new DateTime(2024, 6, 1, 14, 0, 0, DateTimeKind.Utc);

            var workflow = new WorkflowEntity
            {
                Description = "Timestamped E2E",
                CreatedAt = created,
                ModifiedAt = modified,
                ModifiedBy = "e2e-user",
                Rules =
                {
                    new RuleEntity
                    {
                        Description = "R1",
                        Expression = "true",
                        CreatedAt = created,
                        ModifiedAt = modified,
                        ModifiedBy = "rule-user"
                    }
                }
            };

            _db.Workflows.Add(workflow);
            _db.SaveChanges();

            var loaded = _db.Workflows.Find(workflow.Id);
            var domain = loaded!.ToDomainModel();

            domain.CreatedAt.Should().Be(created);
            domain.ModifiedAt.Should().Be(modified);
            domain.ModifiedBy.Should().Be("e2e-user");
            domain.Rules[0].CreatedAt.Should().Be(created);
            domain.Rules[0].ModifiedAt.Should().Be(modified);
            domain.Rules[0].ModifiedBy.Should().Be("rule-user");
        }

        // ==================== Null/Empty Handling ====================

        [Fact]
        public void RuleEntity_ToDomainModel_NullOptionalProperties()
        {
            var entity = new RuleEntity
            {
                Description = "Minimal rule",
                Expression = "true"
            };

            var domain = entity.ToDomainModel();

            domain.Description.Should().Be("Minimal rule");
            domain.Expression.Should().Be("true");
            domain.Action.Should().BeEmpty();
            domain.Timeout.Should().BeNull();
            domain.CacheDuration.Should().BeNull();
            domain.DependsOnRuleId.Should().BeNull();
            domain.ParentRuleId.Should().BeNull();
            domain.WorkflowId.Should().BeNull();
            domain.DescriptionKey.Should().BeNull();
            domain.ModifiedBy.Should().BeNull();
            domain.Version.Should().Be(new RuleVersion(1, 0, 0));
        }

        [Fact]
        public void WorkflowEntity_ToDomainModel_EmptyRules()
        {
            var entity = new WorkflowEntity
            {
                Description = "Empty workflow"
            };

            var domain = entity.ToDomainModel();

            domain.Description.Should().Be("Empty workflow");
            domain.Rules.Should().BeEmpty();
        }
    }

    /// <summary>
    /// In-memory test DbContext for EF Core tests.
    /// </summary>
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<WorkflowEntity> Workflows { get; set; } = null!;
        public DbSet<RuleEntity> Rules { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureRoslynRules();
        }
    }
}
