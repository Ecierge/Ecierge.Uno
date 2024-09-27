namespace Ecierge.Uno.Navigation;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Windows.UI.Core;

internal class BrowserAddressBarService : IHostedService
{
    private readonly ILogger logger;
    private readonly IRouteNotifier notifier;
    private readonly IHasAddressBar? addressbarHost;
    private readonly NavigationOptions? config;
    private Action? unregister;

    public BrowserAddressBarService(
        ILogger<BrowserAddressBarService> logger,
        IRouteNotifier notifier,
        IOptions<NavigationOptions>? config,
        IHasAddressBar? host = null)
    {
        this.logger = logger;
        this.notifier = notifier;
        addressbarHost = host;
        this.config = config?.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTraceMessage($"Starting {nameof(BrowserAddressBarService)}");
        }

        if (addressbarHost is not null && (config?.AddressBarUpdateEnabled ?? true))
        {
            notifier.RouteChanged += RouteChanged;
            unregister = () => notifier.RouteChanged -= RouteChanged;
        }
        else
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebugMessage($"{nameof(IHasAddressBar)} not defined, or {nameof(NavigationOptions.AddressBarUpdateEnabled)} set to false");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTraceMessage($"Starting {nameof(BrowserAddressBarService)}");
        }

        var stopAction = unregister;
        unregister = default;
        stopAction?.Invoke();

        return Task.CompletedTask;
    }


    private async void RouteChanged(object? sender, RouteChangedEventArgs e)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            //var rootRegion = e.Region.Root();
            //var route = rootRegion.GetRoute();
            //if (route is null)
            //{
            //    return;
            //}

            //var canGoBack = rootRegion.Navigator() is { } navigator && await navigator.CanGoBack();

            //var url = new UriBuilder
            //{
            //    Query = route.Query(),
            //    Path = route.FullPath()?.Replace("+", "/")
            //};
            //await addressbarHost!.UpdateAddressBar(url.Uri, canGoBack);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarningMessage($"Error encountered updating address bar on route changed event - {ex.Message}");
            }
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}
