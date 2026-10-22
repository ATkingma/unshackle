using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;
}

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public OrderStateMachine() => InstanceState(x => x.CurrentState);
}
