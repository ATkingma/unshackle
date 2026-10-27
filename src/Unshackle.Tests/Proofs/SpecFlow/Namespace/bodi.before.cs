using BoDi;
using TechTalk.SpecFlow;

namespace Shop.Specs;

public interface IClock
{
}

public class SystemClock : IClock
{
}

[Binding]
public class Hooks
{
    private readonly IObjectContainer _container;

    public Hooks(IObjectContainer container)
    {
        _container = container;
    }

    [BeforeScenario]
    public void Register() => _container.RegisterTypeAs<SystemClock, IClock>();
}
