using RoslynRules.Compiler;
using RoslynRules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoslynRules.Testing
{
    /// <summary>
    /// A declarative test case for a single rule or workflow.
    /// Builds input parameters, sets expectations, and runs assertions.
    /// </summary>
    public class RuleTest
    {
        private readonly string _name;
        private readonly Workflow? _workflow;
        private readonly Rule? _rule;
        private readonly List<RuleParameter> _parameters = new();
        private readonly List<string> _namespaces = new();
        private readonly List<Func<RuleResult, RuleResult>> _assertions = new();
        private Type? _parameterType;
        private object? _parameterValue;
        private bool _expectSuccess = true;
        private bool _expectFailure;
        private Type? _expectedExceptionType;

        /// <summary>
        /// Creates a test for an individual rule.
        /// </summary>        
        public static RuleTest For(Rule rule, string? name = null)
        {
            return new RuleTest(name ?? rule.Description, rule: rule);
        }

        /// <summary>
        /// Creates a test for a workflow.
        /// </summary>
        public static RuleTest For(Workflow workflow, string? name = null)
        {
            return new RuleTest(name ?? workflow.Description, workflow: workflow);
        }

        private RuleTest(string name, Workflow? workflow = null, Rule? rule = null)
        {
            _name = name;
            _workflow = workflow;
            _rule = rule;
        }

        /// <summary>
        /// Sets the single input parameter for the test.
        /// </summary>
        public RuleTest WithInput<T>(string name, T value)
        {
            _parameterType = typeof(T);
            _parameterValue = value;
            _parameters.Add(new RuleParameter(name, typeof(T), value));
            return this;
        }

        /// <summary>
        /// Adds an additional namespace for expression compilation.
        /// </summary>
        public RuleTest WithNamespace(string ns)
        {
            _namespaces.Add(ns);
            return this;
        }

        /// <summary>
        /// Sets up the input using a builder action.
        /// </summary>
        public RuleTest WithInput<T>(string name, Action<T> configure) where T : new()
        {
            var value = new T();
            configure(value);
            return WithInput(name, value);
        }

        // ==================== EXPECTATIONS ====================

        /// <summary>
        /// Expects the rule/workflow to pass.
        /// </summary>
        public RuleTest ExpectSuccess()
        {
            _expectSuccess = true;
            _expectFailure = false;
            return this;
        }

        /// <summary>
        /// Expects the rule/workflow to fail.
        /// </summary>
        public RuleTest ExpectFailure()
        {
            _expectSuccess = false;
            _expectFailure = true;
            return this;
        }

        /// <summary>
        /// Expects the rule/workflow to be inactive (skipped).
        /// </summary>
        public RuleTest ExpectInactive()
        {
            _expectSuccess = false;
            _expectFailure = false;
            return this;
        }

        /// <summary>
        /// Expects a specific exception type to be thrown.
        /// </summary>
        public RuleTest ExpectException<T>() where T : Exception
        {
            _expectedExceptionType = typeof(T);
            return this;
        }

        /// <summary>
        /// Expects the result to have a specific value.
        /// </summary>
        public RuleTest ExpectValue(object? expectedValue)
        {
            _assertions.Add(r => r.ShouldHaveValue(expectedValue));
            return this;
        }

        /// <summary>
        /// Expects all child rules to pass.
        /// </summary>
        public RuleTest ExpectAllChildrenPass()
        {
            _assertions.Add(r => r.ShouldHaveAllChildrenPass());
            return this;
        }

        /// <summary>
        /// Expects at least one child rule to fail.
        /// </summary>
        public RuleTest ExpectChildFailure()
        {
            _assertions.Add(r => r.ShouldHaveChildFailure());
            return this;
        }

        /// <summary>
        /// Expects a specific number of child results.
        /// </summary>
        public RuleTest ExpectChildCount(int count)
        {
            _assertions.Add(r => r.ShouldHaveChildCount(count));
            return this;
        }

        /// <summary>
        /// Adds a custom assertion to the test.
        /// </summary>
        public RuleTest Assert(Func<RuleResult, RuleResult> assertion)
        {
            _assertions.Add(assertion);
            return this;
        }

        // ==================== EXECUTION ====================

        /// <summary>
        /// Runs the test synchronously.
        /// </summary>
        /// <returns>The result of the rule/workflow execution.</returns>
        /// <exception cref="RuleAssertionException">Thrown when an assertion fails.</exception>
        public RuleResult Run()
        {
            if (_workflow != null)
                return RunWorkflow();

            if (_rule != null)
                return RunRule();

            throw new InvalidOperationException("No rule or workflow specified.");
        }

        /// <summary>
        /// Runs the test asynchronously.
        /// </summary>
        /// <exception cref="RuleAssertionException">Thrown when an assertion fails.</exception>
        public async Task<RuleResult> RunAsync()
        {
            if (_workflow != null)
                return await RunWorkflowAsync();

            if (_rule != null)
                return await RunRuleAsync();

            throw new InvalidOperationException("No rule or workflow specified.");
        }

        private RuleResult RunRule()
        {
            if (_rule == null) throw new InvalidOperationException();

            var parameters = _parameters.ToArray();
            var namespaces = _namespaces.ToArray();

            _rule.Validate();

            var compiler = new ExpressionCompiler();
            _rule.Compile(compiler, parameters, namespaces.Length > 0 ? namespaces : null);

            var result = _rule.Execute(parameters);
            return ApplyAssertions(result);
        }

        private RuleResult RunWorkflow()
        {
            if (_workflow == null) throw new InvalidOperationException();

            var parameters = _parameters.ToArray();
            var namespaces = _namespaces.ToArray();

            _workflow.Validate();
            _workflow.Compile(parameters, namespaces.Length > 0 ? namespaces : null);

            var results = _workflow.Execute(parameters).ToList();
            var result = results.Count == 1
                ? results[0]
                : new RuleResult(
                    results.All(r => r.Success || !r.IsActive),
                    Guid.Empty,
                    _workflow.Description,
                    true,
                    ChildResults: results);

            return ApplyAssertions(result);
        }

        private async Task<RuleResult> RunRuleAsync()
        {
            if (_rule == null) throw new InvalidOperationException();

            var parameters = _parameters.ToArray();
            var namespaces = _namespaces.ToArray();

            _rule.Validate();

            var compiler = new ExpressionCompiler();
            _rule.Compile(compiler, parameters, namespaces.Length > 0 ? namespaces : null);

            var result = await _rule.ExecuteAsync(parameters);
            return ApplyAssertions(result);
        }

        private async Task<RuleResult> RunWorkflowAsync()
        {
            if (_workflow == null) throw new InvalidOperationException();

            var parameters = _parameters.ToArray();
            var namespaces = _namespaces.ToArray();

            _workflow.Validate();
            _workflow.Compile(parameters, namespaces.Length > 0 ? namespaces : null);

            var results = new List<RuleResult>();
            await foreach (var r in _workflow.ExecuteAsync(parameters))
            {
                results.Add(r);
            }

            var result = results.Count == 1
                ? results[0]
                : new RuleResult(
                    results.All(r => r.Success || !r.IsActive),
                    Guid.Empty,
                    _workflow.Description,
                    true,
                    ChildResults: results);

            return ApplyAssertions(result);
        }

        private RuleResult ApplyAssertions(RuleResult result)
        {
            // Apply built-in expectations.
            if (_expectSuccess && !result.Success && result.IsActive)
                throw new RuleAssertionException(
                    $"Test '{_name}': expected success, but rule failed.");

            if (_expectFailure && (result.Success || !result.IsActive))
                throw new RuleAssertionException(
                    $"Test '{_name}': expected failure, but rule passed or was inactive.");

            if (_expectedExceptionType != null && result.Exception == null)
                throw new RuleAssertionException(
                    $"Test '{_name}': expected exception '{_expectedExceptionType.Name}', but none was thrown.");

            if (_expectedExceptionType != null && result.Exception != null && !_expectedExceptionType.IsInstanceOfType(result.Exception))
                throw new RuleAssertionException(
                    $"Test '{_name}': expected exception '{_expectedExceptionType.Name}', but got '{result.Exception.GetType().Name}'.");

            // Apply custom assertions.
            foreach (var assertion in _assertions)
            {
                assertion(result);
            }

            return result;
        }

        /// <summary>
        /// Returns a string representation of the test for logging.
        /// </summary>
        public override string ToString() => _name;
    }
}
