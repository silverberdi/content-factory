using System.Runtime.Versioning;

namespace ContentFactory.Api.Tests;

public class RuntimeCompatibilityTests
{
    [Fact]
    public void TestAssemblyTargetsNet10()
    {
        var targetFramework = typeof(RuntimeCompatibilityTests).Assembly
            .GetCustomAttributes(typeof(TargetFrameworkAttribute), inherit: false)
            .Cast<TargetFrameworkAttribute>()
            .Single()
            .FrameworkName;

        Assert.Equal(".NETCoreApp,Version=v10.0", targetFramework);
    }
}
