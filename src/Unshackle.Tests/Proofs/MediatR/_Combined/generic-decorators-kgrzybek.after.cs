using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace Proofs
{
    public interface IDomainEvent
    {
    }

    public class MeetingCreatedDomainEvent : IDomainEvent
    {
    }

    public interface IDomainEventNotification<out TEventType> : INotification
    {
        TEventType DomainEvent { get; }
    }

    public abstract class DomainNotificationBase<T> : IDomainEventNotification<T>
        where T : IDomainEvent
    {
        public DomainNotificationBase(T domainEvent, Guid id)
        {
            this.Id = id;
            this.DomainEvent = domainEvent;
        }

        public T DomainEvent { get; }

        public Guid Id { get; }
    }

    public class MeetingCreatedNotification : DomainNotificationBase<MeetingCreatedDomainEvent>
    {
        public MeetingCreatedNotification(MeetingCreatedDomainEvent domainEvent, Guid id)
            : base(domainEvent, id)
        {
        }
    }

    public interface ICommand : IRequest
    {
    }

    public interface ICommand<out TResult> : IRequest<TResult>
    {
    }

    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
        where TCommand : global::Proofs.ICommand
    {
    }

    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
        where TCommand : global::Proofs.ICommand<TResult>
    {
    }

    public interface IRecurringCommand
    {
    }

    public interface IUnitOfWork
    {
        Task<int> CommitAsync(CancellationToken cancellationToken);
    }

    public interface IDomainEventsDispatcher
    {
        Task DispatchEventsAsync();
    }

    internal class LoggingCommandHandlerDecorator<T> : global::Proofs.ICommandHandler<T>
        where T : global::Proofs.ICommand
    {
        private readonly IRequestHandler<T, Unit> _decorated;

        public LoggingCommandHandlerDecorator(IRequestHandler<T, Unit> decorated)
        {
            _decorated = decorated;
        }

        public async ValueTask<Unit> Handle(T command, CancellationToken cancellationToken)
        {
            if (command is IRecurringCommand)
            {
                await _decorated.Handle(command, cancellationToken);

                return Unit.Value;
            }

            Console.WriteLine("Executing command " + command.GetType().Name);
            await _decorated.Handle(command, cancellationToken);
            return Unit.Value;
        }
    }

    internal class UnitOfWorkCommandHandlerDecorator<T> : global::Proofs.ICommandHandler<T>
        where T : global::Proofs.ICommand
    {
        private readonly IRequestHandler<T, Unit> _decorated;
        private readonly IUnitOfWork _unitOfWork;

        public UnitOfWorkCommandHandlerDecorator(
            IRequestHandler<T, Unit> decorated,
            IUnitOfWork unitOfWork)
        {
            _decorated = decorated;
            _unitOfWork = unitOfWork;
        }

        public async ValueTask<Unit> Handle(T command, CancellationToken cancellationToken)
        {
            await this._decorated.Handle(command, cancellationToken);

            await this._unitOfWork.CommitAsync(cancellationToken);
            return Unit.Value;
        }
    }

    internal class UnitOfWorkCommandHandlerWithResultDecorator<T, TResult> : global::Proofs.ICommandHandler<T, TResult>
        where T : global::Proofs.ICommand<TResult>
    {
        private readonly global::Proofs.ICommandHandler<T, TResult> _decorated;
        private readonly IUnitOfWork _unitOfWork;

        public UnitOfWorkCommandHandlerWithResultDecorator(
            global::Proofs.ICommandHandler<T, TResult> decorated,
            IUnitOfWork unitOfWork)
        {
            _decorated = decorated;
            _unitOfWork = unitOfWork;
        }

        public async ValueTask<TResult> Handle(T command, CancellationToken cancellationToken)
        {
            var result = await this._decorated.Handle(command, cancellationToken);

            await this._unitOfWork.CommitAsync(cancellationToken);

            return result;
        }
    }

    public class DomainEventsDispatcherNotificationHandlerDecorator<T> : INotificationHandler<T>
        where T : INotification
    {
        private readonly INotificationHandler<T> _decorated;
        private readonly IDomainEventsDispatcher _domainEventsDispatcher;

        public DomainEventsDispatcherNotificationHandlerDecorator(
            IDomainEventsDispatcher domainEventsDispatcher,
            INotificationHandler<T> decorated)
        {
            _domainEventsDispatcher = domainEventsDispatcher;
            _decorated = decorated;
        }

        public async ValueTask Handle(T notification, CancellationToken cancellationToken)
        {
            await this._decorated.Handle(notification, cancellationToken);

            await this._domainEventsDispatcher.DispatchEventsAsync();
        }
    }

    public class CreateMeetingCommand : global::Proofs.ICommand
    {
    }

    public class CreateMeetingCommandHandler : global::Proofs.ICommandHandler<CreateMeetingCommand>
    {
        public ValueTask<Unit> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
        {
            return Unit.ValueTask;
        }
    }

    public class MeetingCreatedNotificationHandler : INotificationHandler<MeetingCreatedNotification>
    {
        public ValueTask Handle(MeetingCreatedNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    internal class ProcessingModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGenericDecorator(
                typeof(UnitOfWorkCommandHandlerDecorator<>),
                typeof(IRequestHandler<,>));

            builder.RegisterGenericDecorator(
                typeof(UnitOfWorkCommandHandlerWithResultDecorator<,>),
                typeof(global::Proofs.ICommandHandler<,>));

            builder.RegisterGenericDecorator(
                typeof(LoggingCommandHandlerDecorator<>),
                typeof(IRequestHandler<,>));

            builder.RegisterGenericDecorator(
                typeof(DomainEventsDispatcherNotificationHandlerDecorator<>),
                typeof(INotificationHandler<>));
        }
    }

    public class MediatorModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Mediator's dispatcher; the handlers stay registered by this module, as they were for MediatR.
            var mediatorServices = new ServiceCollection();
            mediatorServices.AddMediator(mediatorOptions =>
            {
                mediatorOptions.ServiceLifetime = ServiceLifetime.Scoped;
                mediatorOptions.NotificationPublisherType = typeof(global::Mediator.TaskWhenAllPublisher);
            });
            builder.Populate(mediatorServices.Where(descriptor => descriptor.ServiceType.Namespace is "Mediator" or "Mediator.Internals"
                && !(descriptor.ServiceType.IsGenericType && descriptor.ServiceType.Assembly == typeof(global::Mediator.IMediator).Assembly)));

            var mediatorOpenTypes = new[]
            {
                typeof(IRequestHandler<,>),
                typeof(INotificationHandler<>),
                typeof(IRequestHandler<>),
                typeof(global::Proofs.ICommandHandler<>),
                typeof(global::Proofs.ICommandHandler<,>),
            };
            foreach (var mediatorOpenType in mediatorOpenTypes)
            {
                builder
                    .RegisterAssemblyTypes(ThisAssembly)
                    .AsClosedTypesOf(mediatorOpenType)
                    .AsImplementedInterfaces();
            }
        }
    }
}
