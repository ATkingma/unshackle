using MediatR;

namespace Proofs;

public abstract record BaseQuery : IRequest<int>;

public record CountQuery<TEntity> : BaseQuery;
