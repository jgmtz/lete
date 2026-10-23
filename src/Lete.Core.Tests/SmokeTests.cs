using Lete.Core.State;

namespace Lete.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void CoreAssembly_HasNoGodotDependency()
    {
        var assembly = typeof(WorldState).Assembly;

        Assert.Equal("Lete.Core", assembly.GetName().Name);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), a => a.Name!.StartsWith("Godot"));
    }
}
