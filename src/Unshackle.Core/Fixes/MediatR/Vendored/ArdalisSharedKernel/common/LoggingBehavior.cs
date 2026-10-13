using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;

namespace Ardalis.SharedKernel
{
  /// <summary>
  /// Adds logging for all requests in the Mediator pipeline.
  /// Configure by adding the service with a scoped lifetime.
  /// Logs under the category "MediatR.Mediator", like the MediatR version of this behavior did.
  /// </summary>
  /// <typeparam name="TRequest"></typeparam>
  /// <typeparam name="TResponse"></typeparam>
  public class LoggingBehavior<TRequest, TResponse> : global::Mediator.IPipelineBehavior<TRequest, TResponse>
    where TRequest : global::Mediator.IRequest<TResponse>
  {
    private readonly ILogger _logger;

    public LoggingBehavior(ILoggerFactory loggerFactory)
    {
      _logger = loggerFactory.CreateLogger("MediatR.Mediator");
    }

    public async ValueTask<TResponse> Handle(TRequest request, global::Mediator.MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
      Guard.Against.Null(request);
      if (_logger.IsEnabled(LogLevel.Information))
      {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);

        // Reflection! Could be a performance concern
        Type myType = request.GetType();
        IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
        foreach (PropertyInfo prop in props)
        {
          object? propValue = prop?.GetValue(request, null);
          _logger.LogInformation("Property {Property} : {@Value}", prop?.Name, propValue);
        }
      }

      var sw = Stopwatch.StartNew();

      var response = await next(request, cancellationToken);

      _logger.LogInformation("Handled {RequestName} with {Response} in {ms} ms", typeof(TRequest).Name, response, sw.ElapsedMilliseconds);
      sw.Stop();
      return response;
    }
  }
}
