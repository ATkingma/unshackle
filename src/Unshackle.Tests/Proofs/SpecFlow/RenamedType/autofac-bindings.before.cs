using Autofac;
using SpecFlow.Autofac;
using SpecFlow.Autofac.SpecFlowPlugin;
using TechTalk.SpecFlow;

namespace Shop.Specs;

public interface IClock
{
}

public class SystemClock : IClock
{
}

public static class Dependencies
{
    [GlobalDependencies]
    public static void SetupGlobal(ContainerBuilder builder) => builder.RegisterType<SystemClock>().As<IClock>().SingleInstance();

    [ScenarioDependencies]
    public static void SetupScenario(ContainerBuilder builder) => builder.AddSpecFlowBindings<Steps>();
}

[Binding]
public class Steps
{
    private readonly IClock _clock;

    public Steps(IClock clock)
    {
        _clock = clock;
    }

    [Given("the clock")]
    public void GivenTheClock() => _ = _clock;
}
