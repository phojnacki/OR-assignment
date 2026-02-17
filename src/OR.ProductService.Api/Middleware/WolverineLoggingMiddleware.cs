using Wolverine;
using Wolverine.Runtime;

namespace OR.ProductService.Api.Middleware;

public static class WolverineLoggingMiddleware
{
    public static void Before(Envelope envelope, ILogger logger)
    {
        logger.LogInformation(
            "Wolverine: Starting to handle {MessageType} (CorrelationId: {CorrelationId})",
            envelope.MessageType, envelope.CorrelationId);
    }

    public static void After(Envelope envelope, ILogger logger)
    {
        logger.LogInformation(
            "Wolverine: Finished handling {MessageType} (CorrelationId: {CorrelationId})",
            envelope.MessageType, envelope.CorrelationId);
    }
}
