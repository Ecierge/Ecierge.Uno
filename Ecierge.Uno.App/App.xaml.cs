namespace Ecierge.Uno.App;

using System.Net.Http;

using Ecierge.Uno.Navigation;

using global::Uno.Extensions;
using global::Uno.Extensions.Configuration;
using global::Uno.Extensions.Hosting;
using global::Uno.Extensions.Localization;
using global::Uno.Resizetizer;
using global::Uno.UI;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                // Register Json serializers (ISerializer and ISerializer)
                .UseSerialization((context, services) => services
                    //.AddContentSerializer(context)
                    .AddJsonTypeInfo(WeatherForecastContext.Default.IImmutableListWeatherForecast))
                .UseHttp((context, services) => services
                    // Register HttpClient
#if DEBUG
                    // DelegatingHandler will be automatically injected into HTTP Client
                    .AddTransient<DelegatingHandler, DebugHttpHandler>()
#endif
                )
                .ConfigureServices((context, services) =>
                {
                    // TODO: Register your services
                    //services.AddSingleton<IMyService, MyService>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = builder.Build();
        _ = Host.RunAsync();
        await MainWindow.NavigateAsync<Shell>(Host.Services, null,//async (services, navigator) =>
        //{

        //},
        shell => shell.ContentControl);
    }

    private static void RegisterRoutes(IViewRegistryBuilder views, INavigationDataRegistryBuilder data, IRouteRegistryBuilder routes)
    {
        views.Register(
            new ViewMap<Shell, ShellViewModel>(),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<MainContentDialog, MainViewModel>(),
            new ViewMap<SecondPage, SecondViewModel>()
        );

        data.Register<EntityNavigationDataMap>();

        routes.Register((views, data) => [
            //new NameSegment("", views[typeof(Shell)],
            //    nested:
            //    [
                    new ("Main", views[typeof(MainPage)], isDefault:true, [
                            new ("Tab1", isDefault: true),
                            new ("Tab2"),
                            new DialogSegment("Dialog", views[typeof(MainPage)], nested:[
                                    new ("Tab1", isDefault: true),
                                    new ("Tab2"),
                                ])
                        ]),
                    new ("Second", views[typeof(SecondPage)], new DataSegment("name", data[typeof(Entity)])),
                    new DialogSegment("ContentDialog", views[typeof(MainContentDialog)], nested:[
                            new ("Tab1", isDefault: true),
                            new ("Tab2"),
                        ])
            //    ]
            //)
            ]
        );
    }
}
