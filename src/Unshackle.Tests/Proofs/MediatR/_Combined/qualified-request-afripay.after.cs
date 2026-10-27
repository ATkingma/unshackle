using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proofs
{
    public record GetMerchantQuery(Guid MerchantId)
        : Mediator.IRequest<string>;

    public class GetMerchantHandler : Mediator.IRequestHandler<GetMerchantQuery, string>
    {
        public ValueTask<string> Handle(GetMerchantQuery request, CancellationToken cancellationToken) => ValueTask.FromResult(request.MerchantId.ToString());
    }
}
