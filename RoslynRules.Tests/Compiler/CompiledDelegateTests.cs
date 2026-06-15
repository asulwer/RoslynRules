using RoslynRules.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace RoslynRules.Tests.Compiler
{
    /// <summary>
    /// Tests for CompiledDelegate classes and factory to cover all wrapping paths.
    /// </summary>
    public class CompiledDelegateTests
    {
        [Fact]
        public void CompiledAction_Invoke_Executes()
        {
            bool executed = false;
            Action<object> action = (obj) => executed = true;
            var compiled = new CompiledAction<object>(action);

            var result = compiled.Invoke("test");

            Assert.True(executed);
            Assert.Null(result);
        }

        [Fact]
        public void CompiledAsyncAction_Invoke_BlocksAndExecutes()
        {
            bool executed = false;
            Func<object, Task> asyncAction = async (obj) =>
            {
                await Task.Delay(1);
                executed = true;
            };
            var compiled = new CompiledAsyncAction<object>(asyncAction);

            var result = compiled.Invoke("test");

            Assert.True(executed);
            Assert.Null(result);
        }

        [Fact]
        public async Task CompiledAsyncAction_InvokeAsync_Awaits()
        {
            bool executed = false;
            Func<object, Task> asyncAction = async (obj) =>
            {
                await Task.Delay(1);
                executed = true;
            };
            var compiled = new CompiledAsyncAction<object>(asyncAction);

            var result = await compiled.InvokeAsync("test");

            Assert.True(executed);
            Assert.Null(result);
        }

        [Fact]
        public void CompiledDelegateFactory_Wrap_SyncAction_ReturnsCompiledAction()
        {
            Action<object> action = (obj) => { };
            var result = CompiledDelegateFactory.Wrap(action);

            Assert.IsType<CompiledAction<object>>(result);
        }

        [Fact]
        public void CompiledDelegateFactory_Wrap_AsyncAction_ReturnsCompiledAsyncAction()
        {
            Func<object, Task> asyncAction = async (obj) => await Task.Delay(1);
            var result = CompiledDelegateFactory.Wrap(asyncAction);

            Assert.IsType<CompiledAsyncAction<object>>(result);
        }

        [Fact]
        public void CompiledDelegateFactory_Wrap_MultiParameter_ReturnsCompiledMultiParamDelegate()
        {
            Func<object, object, bool> func = (a, b) => true;
            var result = CompiledDelegateFactory.Wrap(func);
            Assert.IsType<CompiledMultiParamDelegate>(result);
        }
    }
}
