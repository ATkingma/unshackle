using System;
using Mediator;

namespace Proofs;

public record GetMerchantQuery(Guid MerchantId)
    : Mediator.IRequest<string>;

public record MerchantCreated(Guid MerchantId) : global::Mediator.INotification;
