using FluentAssertions;
using RoslynRules.Models;
using RoslynRules.Testing;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Integration
{
    public class RuleTestingFrameworkTests
    {
        private readonly RuleParameter[] _parameters;

        public RuleTestingFrameworkTests()
        {
            _parameters = new[]
            {
                new RuleParameter("customer", typeof(TestCustomer), new TestCustomer { Age = 25, Name = "Alice" })
            };
        }

        // ==================== RuleResultAssertions ====================

        [Fact]
        public void ShouldPass_ActivePassingRule_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test", IsActive: true);
            result.ShouldPass();
        }

        [Fact]
        public void ShouldPass_ActiveFailingRule_Throws()
        {
            var result = new RuleResult(false, RuleDescription: "Test", IsActive: true);
            var act = () => result.ShouldPass();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected rule 'Test' to pass*");
        }

        [Fact]
        public void ShouldPass_InactiveRule_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", IsActive: false);
            var act = () => result.ShouldPass();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*inactive*");
        }

        [Fact]
        public void ShouldFail_ActiveFailingRule_DoesNotThrow()
        {
            var result = new RuleResult(false, RuleDescription: "Test", IsActive: true);
            result.ShouldFail();
        }

        [Fact]
        public void ShouldFail_ActivePassingRule_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", IsActive: true);
            var act = () => result.ShouldFail();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected rule 'Test' to fail*");
        }

        [Fact]
        public void ShouldBeInactive_InactiveRule_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test", IsActive: false);
            result.ShouldBeInactive();
        }

        [Fact]
        public void ShouldBeInactive_ActiveRule_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", IsActive: true);
            var act = () => result.ShouldBeInactive();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected rule 'Test' to be inactive*");
        }

        [Fact]
        public void ShouldHaveValue_MatchingValue_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: 42);
            result.ShouldHaveValue(42);
        }

        [Fact]
        public void ShouldHaveValue_MismatchedValue_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: 42);
            var act = () => result.ShouldHaveValue(99);
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected rule 'Test' to return '99'*");
        }

        [Fact]
        public void ShouldHaveValue_NonNullValue_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: "hello");
            result.ShouldHaveValue();
        }

        [Fact]
        public void ShouldHaveValue_NullValue_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: null);
            var act = () => result.ShouldHaveValue();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*was null*");
        }

        [Fact]
        public void ShouldHaveValueOfType_CorrectType_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: 42);
            result.ShouldHaveValueOfType<int>();
        }

        [Fact]
        public void ShouldHaveValueOfType_WrongType_Throws()
        {
            var result = new RuleResult(true, RuleDescription: "Test", Value: "hello");
            var act = () => result.ShouldHaveValueOfType<int>();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected rule 'Test' to return type 'Int32'*");
        }

        [Fact]
        public void ShouldHaveAllChildrenPass_AllPass_DoesNotThrow()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(true, RuleDescription: "Child2")
            };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            result.ShouldHaveAllChildrenPass();
        }

        [Fact]
        public void ShouldHaveAllChildrenPass_OneFails_Throws()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(false, RuleDescription: "Child2")
            };
            var result = new RuleResult(false, RuleDescription: "Parent", ChildResults: children);
            var act = () => result.ShouldHaveAllChildrenPass();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Child2*");
        }

        [Fact]
        public void ShouldHaveChildFailure_HasFailure_DoesNotThrow()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(false, RuleDescription: "Child2")
            };
            var result = new RuleResult(false, RuleDescription: "Parent", ChildResults: children);
            result.ShouldHaveChildFailure();
        }

        [Fact]
        public void ShouldHaveChildFailure_AllPass_Throws()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(true, RuleDescription: "Child2")
            };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            var act = () => result.ShouldHaveChildFailure();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*all passed*");
        }

        [Fact]
        public void ShouldHaveChildCount_CorrectCount_DoesNotThrow()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(true, RuleDescription: "Child2")
            };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            result.ShouldHaveChildCount(2);
        }

        [Fact]
        public void ShouldHaveChildCount_WrongCount_Throws()
        {
            var children = new[] { new RuleResult(true, RuleDescription: "Child1") };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            var act = () => result.ShouldHaveChildCount(5);
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Expected 'Parent' to have 5 child results*");
        }

        [Fact]
        public void ShouldHaveChild_Found_ReturnsChild()
        {
            var children = new[]
            {
                new RuleResult(true, RuleDescription: "Child1"),
                new RuleResult(true, RuleDescription: "Child2")
            };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            var child = result.ShouldHaveChild("Child2");
            child.RuleDescription.Should().Be("Child2");
        }

        [Fact]
        public void ShouldHaveChild_NotFound_Throws()
        {
            var children = new[] { new RuleResult(true, RuleDescription: "Child1") };
            var result = new RuleResult(true, RuleDescription: "Parent", ChildResults: children);
            var act = () => result.ShouldHaveChild("Missing");
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Missing*");
        }

        [Fact]
        public void ShouldHaveThrown_CorrectType_DoesNotThrow()
        {
            var result = new RuleResult(false, RuleDescription: "Test", Exception: new InvalidOperationException("boom"));
            result.ShouldHaveThrown<InvalidOperationException>();
        }

        [Fact]
        public void ShouldHaveThrown_WrongType_Throws()
        {
            var result = new RuleResult(false, RuleDescription: "Test", Exception: new ArgumentException("bad"));
            var act = () => result.ShouldHaveThrown<InvalidOperationException>();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*InvalidOperationException*");
        }

        [Fact]
        public void ShouldNotHaveThrown_NoException_DoesNotThrow()
        {
            var result = new RuleResult(true, RuleDescription: "Test");
            result.ShouldNotHaveThrown();
        }

        [Fact]
        public void ShouldNotHaveThrown_Exception_Throws()
        {
            var result = new RuleResult(false, RuleDescription: "Test", Exception: new Exception("oops"));
            var act = () => result.ShouldNotHaveThrown();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*oops*");
        }

        // ==================== Collection Assertions ====================

        [Fact]
        public void ShouldAllPass_AllPass_DoesNotThrow()
        {
            var results = new[]
            {
                new RuleResult(true, RuleDescription: "R1"),
                new RuleResult(true, RuleDescription: "R2")
            };
            results.ShouldAllPass();
        }

        [Fact]
        public void ShouldAllPass_OneFails_Throws()
        {
            var results = new[]
            {
                new RuleResult(true, RuleDescription: "R1"),
                new RuleResult(false, RuleDescription: "R2")
            };
            var act = () => results.ShouldAllPass();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*R2*");
        }

        [Fact]
        public void ShouldHaveAnyFailure_HasFailure_DoesNotThrow()
        {
            var results = new[]
            {
                new RuleResult(true, RuleDescription: "R1"),
                new RuleResult(false, RuleDescription: "R2")
            };
            results.ShouldHaveAnyFailure();
        }

        [Fact]
        public void ShouldHaveAnyFailure_AllPass_Throws()
        {
            var results = new[]
            {
                new RuleResult(true, RuleDescription: "R1"),
                new RuleResult(true, RuleDescription: "R2")
            };
            var act = () => results.ShouldHaveAnyFailure();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*all passed*");
        }

        [Fact]
        public void ShouldHaveCount_CorrectCount_DoesNotThrow()
        {
            var results = new[] { new RuleResult(true), new RuleResult(true) };
            results.ShouldHaveCount(2);
        }

        [Fact]
        public void ShouldContainRule_Found_ReturnsResult()
        {
            var results = new[]
            {
                new RuleResult(true, RuleDescription: "Rule1"),
                new RuleResult(true, RuleDescription: "Rule2")
            };
            var found = results.ShouldContainRule("Rule2");
            found.RuleDescription.Should().Be("Rule2");
        }

        // ==================== RuleTest ====================

        [Fact]
        public void RuleTest_ForRule_ExpectSuccess_Passes()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var test = RuleTest.For(rule)
                .WithInput("customer", new TestCustomer { Age = 25, Name = "Alice" })
                .ExpectSuccess();

            var result = test.Run();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void RuleTest_ForRule_ExpectFailure_Fails()
        {
            var rule = new Rule
            {
                Description = "Adult check",
                Expression = "customer.Age >= 18",
                IsActive = true
            };

            var test = RuleTest.For(rule)
                .WithInput("customer", new TestCustomer { Age = 16, Name = "Bob" })
                .ExpectFailure();

            var result = test.Run();
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void RuleTest_ForRule_WithBuilder_ConfigureInput()
        {
            var rule = new Rule
            {
                Description = "Name check",
                Expression = "customer.Name.StartsWith(\"A\")",
                IsActive = true
            };

            var test = RuleTest.For(rule)
                .WithInput("customer", (TestCustomer c) => { c.Name = "Alice"; c.Age = 25; })
                .ExpectSuccess();

            var result = test.Run();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void RuleTest_ForRule_CustomAssertion()
        {
            var rule = new Rule
            {
                Description = "Set adult",
                Expression = "customer.Age >= 18",
                Action = "customer.IsAdult = true",
                IsActive = true
            };

            var customer = new TestCustomer { Age = 25, Name = "Alice" };
            var test = RuleTest.For(rule)
                .WithInput("customer", customer)
                .ExpectSuccess()
                .Assert(r => { customer.IsAdult.Should().BeTrue(); return r; });

            test.Run();
        }

        [Fact]
        public void RuleTest_ForWorkflow_ExpectSuccess_Passes()
        {
            var workflow = new Workflow
            {
                Description = "Customer validation",
                Rules = new List<Rule>
                {
                    new Rule { Description = "Adult", Expression = "customer.Age >= 18", IsActive = true },
                    new Rule { Description = "Named", Expression = "!string.IsNullOrEmpty(customer.Name)", IsActive = true }
                }
            };

            var test = RuleTest.For(workflow)
                .WithInput("customer", new TestCustomer { Age = 25, Name = "Alice" })
                .ExpectSuccess();

            var result = test.Run();
            result.Success.Should().BeTrue();
        }

        [Fact]
        public void RuleTest_ForRule_WithChildRules_AssertionsWork()
        {
            var parent = new Rule
            {
                Description = "Parent",
                Expression = "true",
                IsActive = true
            };

            var child1 = new Rule { Description = "Adult", Expression = "customer.Age >= 18", IsActive = true };
            var child2 = new Rule { Description = "Named", Expression = "!string.IsNullOrEmpty(customer.Name)", IsActive = true };
            parent.ChildRules.Add(child1);
            parent.ChildRules.Add(child2);

            var test = RuleTest.For(parent)
                .WithInput("customer", new TestCustomer { Age = 25, Name = "Alice" })
                .ExpectSuccess()
                .ExpectAllChildrenPass()
                .ExpectChildCount(2);

            var result = test.Run();
            result.Success.Should().BeTrue();
        }

        // ==================== RuleTestSuite ====================

        [Fact]
        public void RuleTestSuite_MultipleTests_AllPass()
        {
            var suite = new RuleTestSuite()
                .AddTest(RuleTest.For(new Rule { Description = "R1", Expression = "customer.Age >= 18", IsActive = true })
                    .WithInput("customer", new TestCustomer { Age = 25 })
                    .ExpectSuccess())
                .AddTest(RuleTest.For(new Rule { Description = "R2", Expression = "customer.Age < 18", IsActive = true })
                    .WithInput("customer", new TestCustomer { Age = 16 })
                    .ExpectSuccess());

            var result = suite.Run();
            result.AllPassed.Should().BeTrue();
            result.PassedCount.Should().Be(2);
        }

        [Fact]
        public void RuleTestSuite_MultipleTests_OneFails()
        {
            var suite = new RuleTestSuite()
                .AddTest(RuleTest.For(new Rule { Description = "R1", Expression = "customer.Age >= 18", IsActive = true })
                    .WithInput("customer", new TestCustomer { Age = 25 })
                    .ExpectSuccess())
                .AddTest(RuleTest.For(new Rule { Description = "R2", Expression = "customer.Age >= 18", IsActive = true })
                    .WithInput("customer", new TestCustomer { Age = 16 })
                    .ExpectSuccess()); // This will fail

            var result = suite.Run();
            result.AllPassed.Should().BeFalse();
            result.PassedCount.Should().Be(1);
            result.FailedCount.Should().Be(1);
        }

        [Fact]
        public void RuleTestSuiteResult_ThrowOnFailure_WithFailures_Throws()
        {
            var suite = new RuleTestSuite()
                .AddTest(RuleTest.For(new Rule { Description = "R1", Expression = "customer.Age >= 18", IsActive = true })
                    .WithInput("customer", new TestCustomer { Age = 16 })
                    .ExpectSuccess());

            var result = suite.Run();
            var act = () => result.ThrowOnFailure();
            act.Should().Throw<RuleAssertionException>()
                .WithMessage("*Rule test suite failed*");
        }

        [Fact]
        public void RuleTestSuiteResult_ToString_FormatsSummary()
        {
            var suite = new RuleTestSuite()
                .AddTest(RuleTest.For(new Rule { Description = "Passing", Expression = "true", IsActive = true })
                    .WithInput("customer", new TestCustomer())
                    .ExpectSuccess());

            var result = suite.Run();
            var summary = result.ToString();
            summary.Should().Contain("1 passed, 0 failed");
            summary.Should().Contain("PASS");
        }
    }
}