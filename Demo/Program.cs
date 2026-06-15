using Demo.Data;
using Demo.Demos;

namespace Demo;

internal class Program
{
    static async Task Main(string[] args)
    {
        DemoRunner.LoadCustomers();
        await SeedData.InitializeAsync();

        await DemoRunner.Run("Basic Predicates", BasicPredicatesDemo.Run);
        await DemoRunner.Run("Rule Chaining", RuleChainingDemo.Run);
        await DemoRunner.Run("Child Rules", ChildRulesDemo.Run);
        await DemoRunner.Run("Async Expressions", AsyncExpressionsDemo.Run);
        await DemoRunner.Run("Custom Types", CustomTypesDemo.Run);
        await DemoRunner.Run("ExpandoObject", ExpandoObjectDemo.Run);
        await DemoRunner.Run("JSON Round-Trip", JsonRoundTripDemo.Run);
        await DemoRunner.Run("Workflow vs RuleBatch", WorkflowVsRuleBatchDemo.Run);
        await DemoRunner.Run("Template Instantiation", TemplateInstantiationDemo.Run);
        await DemoRunner.Run("Multi-Parameter", MultiParameterDemo.Run);
        await DemoRunner.Run("Caching", CachingDemo.Run);
        await DemoRunner.Run("Priority Ordering", PriorityOrderingDemo.Run);
        await DemoRunner.Run("Entity Framework", EntityFrameworkDemo.Run);
        await DemoRunner.Run("Workflow Compile", WorkflowCompileDemo.Run);
        await DemoRunner.Run("Lifecycle Events", LifecycleEventsDemo.Run);

        Console.WriteLine();
        Console.WriteLine("=== All demos completed ===");
    }
}
