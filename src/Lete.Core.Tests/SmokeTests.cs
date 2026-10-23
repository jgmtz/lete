using Lete.Core;

namespace Lete.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void CoreAssembly_IsReferencedAndHasNoGodotDependency()
    {
        var assembly = typeof(CoreAssembly).Assembly;

        Assert.Equal(CoreAssembly.Name, assembly.GetName().Name);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Godot"));
    }
}
