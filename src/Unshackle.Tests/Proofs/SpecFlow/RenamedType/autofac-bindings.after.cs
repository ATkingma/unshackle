using Autofac;
using Reqnroll.Autofac;
using Reqnroll.Autofac.ReqnrollPlugin;
using Reqnroll;

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
    public static void SetupScenario(ContainerBuilder builder) => builder.AddReqnrollBindings<Steps>();
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
