using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proofs
{
    public record GetMerchantQuery(Guid MerchantId)
        : MediatR.IRequest<string>;

    public class GetMerchantHandler : MediatR.IRequestHandler<GetMerchantQuery, string>
    {
        public Task<string> Handle(GetMerchantQuery request, CancellationToken cancellationToken) => Task.FromResult(request.MerchantId.ToString());
    }
}
