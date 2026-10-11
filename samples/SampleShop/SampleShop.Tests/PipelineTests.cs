using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SampleShop.Data;
using SampleShop.Domain;
using SampleShop.Dtos;
using SampleShop.Features.Common;
using SampleShop.Features.Customers;
using SampleShop.Features.Orders;

namespace SampleShop.Tests;

// MediatR constructions beyond plain requests; only simple assertions so the FA fix can migrate this file fully.
public class PipelineTests
{
    private readonly ServiceProvider _provider = new ServiceCollection().AddSampleShop().BuildServiceProvider();

    private ISender Sender => _provider.GetRequiredService<ISender>();

    private List<string> Log => _provider.GetRequiredService<AuditLog>().Entries;

    private async Task<int> PlaceBookOrderAsync() =>
        await Sender.Send(new PlaceOrderCommand("Timme", [new OrderLineDto { ProductName = "Book", Quantity = 1, UnitPrice = 10m }]), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Stream_YieldsMappedCustomers_AndRunsStreamBehavior()
    {
        await Sender.Send(new RegisterCustomerCommand("Ada", "Lovelace", "London"), TestContext.Current.CancellationToken);
        await Sender.Send(new RegisterCustomerCommand("Alan", "Turing", "Manchester"), TestContext.Current.CancellationToken);

        var customers = new List<CustomerDto>();
        await foreach (var customer in Sender.CreateStream(new StreamCustomersQuery("London"), TestContext.Current.CancellationToken))
        {
            customers.Add(customer);
        }

        customers.Should().HaveCount(1);
        customers[0].FullName.Should().Be("Ada Lovelace");
        Log.Should().Contain("Streaming StreamCustomersQuery");
    }

    [Fact]
    public async Task UnitCommand_ReturnsUnit_AndRunsPreAndPostProcessors()
    {
        var result = await Sender.Send(new RegisterCustomerCommand("Ada", "Lovelace", "London"), TestContext.Current.CancellationToken);

        result.Should().Be(Unit.Value);
        Log.Should().Contain("Pre RegisterCustomerCommand");
        Log.Should().Contain("Post RegisterCustomerCommand");
    }

    [Fact]
    public async Task ValidationBehavior_RejectsMissingLastName()
    {
        Func<Task> act = () => Sender.Send(new RegisterCustomerCommand("Ada", "", "London"), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Last name is required*");
    }

    [Fact]
    public async Task GenericHandler_CountsPerEntityType()
    {
        await PlaceBookOrderAsync();

        var orders = await Sender.Send(new CountQuery<Order>(), TestContext.Current.CancellationToken);
        var customers = await Sender.Send(new CountQuery<Customer>(), TestContext.Current.CancellationToken);

        orders.Should().Be(1);
        customers.Should().Be(0);
    }

    [Fact]
    public async Task VoidCommand_PublishesToAllHandlers_ViaFacade()
    {
        var id = await PlaceBookOrderAsync();
        var facade = _provider.GetRequiredService<OrderFacade>();

        await facade.CancelAsync(id, TestContext.Current.CancellationToken);

        Log.Should().Contain($"Order {id} cancelled");
        Log.Should().Contain($"Mail sent for order {id}");
        (await facade.CountOrdersAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task ExceptionHandler_HandlesUnknownOrder()
    {
        await Sender.Send(new CancelOrderCommand(99), TestContext.Current.CancellationToken);

        Log.Should().Contain("Not found: 99");
    }

    [Fact]
    public void ObsoleteApi_StillWorks()
    {
        var facade = _provider.GetRequiredService<OrderFacade>();

        facade.PriceWithVat(100m).Should().Be(121m);
    }
}
