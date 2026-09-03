using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Application.Reservations;

namespace PharmacyAvailability.Api.BackgroundServices;

public class ReservationExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReservationOptions _options;
    private readonly ILogger<ReservationExpirationWorker> _logger;

    public ReservationExpirationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ReservationOptions> options,
        ILogger<ReservationExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpirationWorker started. Check interval: {Interval}s",
            _options.ExpirationWorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                    await reservationService.ExpireOverdueReservationsAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred in ReservationExpirationWorker sweep.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_options.ExpirationWorkerIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("ReservationExpirationWorker stopped.");
    }
}
