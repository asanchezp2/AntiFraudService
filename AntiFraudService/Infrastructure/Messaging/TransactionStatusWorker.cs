using AntiFraudService.Application.Commands.UpdateTransaction;
using AntiFraudService.Domain.Interfaces;
using Application.Commands.UpdateTransaction;

namespace Infrastructure.Messaging;

public class TransactionStatusWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TransactionStatusWorker> _logger;
    private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _maxDelay = TimeSpan.FromMinutes(5);

    public TransactionStatusWorker(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TransactionStatusWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TransactionStatusWorker started");

        var currentDelay = _baseDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            IServiceScope scope = null;
            ITransactionConsumer consumer = null;

            try
            {
                scope = _serviceScopeFactory.CreateScope();
                consumer = scope.ServiceProvider.GetRequiredService<ITransactionConsumer>();
                var handler = scope.ServiceProvider.GetRequiredService<UpdateTransactionHandler>();

                await consumer.ConsumeAsync(async (msgDto) =>
                {
                    try
                    {
                        var command = new UpdateTransactionCommand
                        {
                            TransactionId = msgDto.TransactionId,
                            SourceAccountId = msgDto.SourceAccountId,
                            TargetAccountId = msgDto.TargetAccountId,
                            Value = msgDto.Value
                        };

                        await handler.Handle(command, stoppingToken);

                        _logger.LogDebug("Successfully processed transaction {TransactionId}", msgDto.TransactionId);

                        currentDelay = _baseDelay;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing transaction {TransactionId}",
                            msgDto?.TransactionId);
                    }
                }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("TransactionStatusWorker cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TransactionStatusWorker, will retry in {Delay}", currentDelay);

                try
                {
                    await Task.Delay(currentDelay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                // Exponential backoff con límite máximo
                currentDelay = TimeSpan.FromMilliseconds(
                    Math.Min(currentDelay.TotalMilliseconds * 2, _maxDelay.TotalMilliseconds));
            }
            finally
            {
                if (consumer != null)
                {
                    try
                    {
                        await consumer.CloseAsync(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error closing consumer");
                    }
                }

                scope?.Dispose();
            }
        }

        _logger.LogInformation("TransactionStatusWorker stopped");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TransactionStatusWorker stopping...");
        await base.StopAsync(cancellationToken);
    }
}