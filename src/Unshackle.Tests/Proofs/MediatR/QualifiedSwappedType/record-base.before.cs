using System;

namespace Proofs;

public record GetMerchantQuery(Guid MerchantId)
    : MediatR.IRequest<string>;

public record MerchantCreated(Guid MerchantId) : global::MediatR.INotification;
