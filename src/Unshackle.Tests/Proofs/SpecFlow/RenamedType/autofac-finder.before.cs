using System;
using Autofac;
using SpecFlow.Autofac;

namespace Shop.Specs;

public class Finder
{
    public Func<ContainerBuilder, ContainerBuilder>? ScenarioBuilder(IContainerBuilderFinder finder) => finder.GetCreateScenarioContainerBuilder();
}
