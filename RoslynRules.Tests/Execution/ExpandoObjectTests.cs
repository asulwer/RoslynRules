using FluentAssertions;
using RoslynRules.Compiler;
using Microsoft.CSharp.RuntimeBinder;
using RoslynRules.Models;
using System.Dynamic;
using Xunit;
using Workflow = global::RoslynRules.Models.Workflow;

namespace RoslynRules.Tests.Execution
{
    /// <summary>
    /// Tests proving ExpandoObject support via dynamic expressions.
    /// ExpandoObject is slower than typed objects but useful when the shape
    /// is not known at compile time.
    /// </summary>
    public class ExpandoObjectTests
    {
        private readonly string[] _namespaces;

        private readonly ExpressionCompiler _compiler;

        public ExpandoObjectTests()
        {
            _compiler = TestCompiler.Instance;
            _namespaces = new[] { "System.Dynamic", "Microsoft.CSharp.RuntimeBinder" };
        }

        [Fact]
        public void Execute_ExpandoObject_SimpleProperty_ReturnsTrue()
        {
            dynamic customer = new ExpandoObject();
            customer.Name = "Alice";
            customer.Age = 25;

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var rule = new Rule
            {
                Description = "Adult check with ExpandoObject",
                Expression = "((dynamic)customer).Age >= 18",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_ExpandoObject_PropertyEquals_ReturnsTrue()
        {
            dynamic customer = new ExpandoObject();
            customer.Name = "Alice";

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var rule = new Rule
            {
                Description = "Name check",
                Expression = "((dynamic)customer).Name == \"Alice\"",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_ExpandoObject_NestedProperty_ReturnsTrue()
        {
            dynamic customer = new ExpandoObject();
            customer.Address = new ExpandoObject();
            customer.Address.City = "Phoenix";

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var rule = new Rule
            {
                Description = "City check",
                Expression = "((dynamic)customer).Address.City == \"Phoenix\"",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
        }

        [Fact]
        public void Execute_ExpandoObject_MissingProperty_ReturnsFalse()
        {
            dynamic customer = new ExpandoObject();
            customer.Name = "Alice";
            // Note: customer.Age is NOT set (returns null)

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var rule = new Rule
            {
                Description = "Missing property",
                Expression = "((dynamic)customer).Age >= 18",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            // Missing property returns null, so comparison returns false
            var result = rule.Execute(parameters);
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Execute_ExpandoObject_Action_ModifiesProperty()
        {
            dynamic customer = new ExpandoObject();
            customer.Name = "Alice";
            customer.IsAdult = false;

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var rule = new Rule
            {
                Description = "Mark adult",
                Expression = "((dynamic)customer).Name == \"Alice\"",
                Action = "((dynamic)customer).IsAdult = true",
                IsActive = true
            };

                        rule.Compile(_compiler, parameters, _namespaces);

            var result = rule.Execute(parameters);

            result.Success.Should().BeTrue();
            Assert.True(customer.IsAdult);
        }

        [Fact]
        public void Execute_ExpandoObject_Workflow_MultipleRules()
        {
            dynamic customer = new ExpandoObject();
            customer.Name = "Alice";
            customer.Age = 25;

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var workflow = new Workflow
            {
                Description = "Expando workflow",
                Rules = new List<Rule>
                {
                    new Rule
                    {
                        Description = "Name check",
                        Expression = "((dynamic)customer).Name == \"Alice\"",
                        IsActive = true
                    },
                    new Rule
                    {
                        Description = "Age check",
                        Expression = "((dynamic)customer).Age >= 18",
                        IsActive = true
                    }
                }
            };

            workflow.Validate();
            workflow.Compile(parameters, _namespaces);

            var results = workflow.Execute(parameters).ToList();

            results.Should().HaveCount(2);
            results.All(r => r.Success).Should().BeTrue();
        }

        [Fact]
        public void Execute_ExpandoObject_ParentWithChild_BottomUp()
        {
            dynamic customer = new ExpandoObject();
            customer.Age = 25;
            customer.IsVerified = true;

            var parameters = new[]
            {
                new RuleParameter("customer", typeof(object), customer)
            };

            var parent = new Rule
            {
                Description = "Parent",
                Expression = "((dynamic)customer).IsVerified == true",
                IsActive = true
            };

            var child = new Rule
            {
                Description = "Child",
                Expression = "((dynamic)customer).Age >= 18",
                IsActive = true
            };

            parent.ChildRules.Add(child);

                        parent.Compile(_compiler, parameters, _namespaces);

            var result = parent.Execute(parameters);

            result.Success.Should().BeTrue();
        }
    }
}



