namespace Ardalis.SharedKernel
{
  /// <summary>
  /// Source: https://code-maze.com/cqrs-mediatr-fluentvalidation/
  /// </summary>
  /// <typeparam name="TQuery"></typeparam>
  /// <typeparam name="TResponse"></typeparam>
  public interface IQueryHandler<in TQuery, TResponse> : global::Mediator.IRequestHandler<TQuery, TResponse>
         where TQuery : IQuery<TResponse>
  {
  }
}
