using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model.Assets;
using Model.Core;
using Model.DataAccess;
using Model.Entities;
using Model.EventArgs;
using Model.Stats;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services;
using Shared.Services.Configuration;
using Shared.Services.Helpers;
using Shared.Services.Registry;
using Shared.Services.Serializer;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using View;
using View.Services;
using ViewModel;
using Windows.ApplicationModel;

[assembly: SupportedOSPlatform("windows10.0.10240.0")]

namespace Bootstrap
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            string appPath = DetermineAppPath();

            using var appHost = BuildAppHost(appPath);
            InitializeStaticLoggers(appHost);
            var app = appHost.Services.GetRequiredService<App>();
            app.Host = appHost;
            app.InitializeComponent();
            app.Run();
        }


        public static string DetermineAppPath()
        {
            // Check if application is running as MSIX package (ie, was installed and running after public distribution)
            // If so, it's containerized and Environment.CurrentDirectory won't work.
            try
            {
                return Package.Current.InstalledLocation.Path;
            }
            catch (Exception)
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            }
        }

        private static protected IHost BuildAppHost(string appPath)
        {
            var host = Host.CreateDefaultBuilder();

            return host.ConfigureAppConfiguration((context, config) =>
                   {
                       config.SetBasePath(appPath);
                       config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                           .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                   })
                   .ConfigureLogging((context, logging) =>
                   {
                       logging.ClearProviders();
                       logging.AddConsole();
                       logging.AddDebug();
                       if (context.HostingEnvironment.IsDevelopment())
                           logging.AddFilter(logLevel => logLevel >= LogLevel.Trace);
                       else
                           logging.AddFilter(logLevel => logLevel >= LogLevel.Information);
                   })
                   .ConfigureServices((context, services) =>
                   {
                       services.AddSingleton<IPostConfigureOptions<AppConfig>, PostConfigurer>();
                       services.Configure<AppConfig>(context.Configuration);
                       services.AddSingleton<IRegistryInitializer, RegistryInitializer>();
                       services.AddTransient<ITypeRelations, TypeRelations>();
                       services.AddSingleton<ITypeRegister<ITypeRelations>, TypeRegister>();
                       services.AddSingleton<IDataProvider, DataProvider>();
                       services.AddTransient<IAssetFetcher, AssetFetcher>();
                       services.AddTransient<IAssetFactory, AssetFactory>();
                       services.AddTransient<IRuleValues, RuleValues>();
                       services.AddTransient<IGameService, GameService>();
                       services.AddTransient<ITerritoryChangedEventArgs, TerritoryChangedEventArgs>();
                       services.AddTransient<IContinentOwnerChangedEventArgs, ContinentOwnerChangedEventArgs>();
                       services.AddTransient<IRuleValues, RuleValues>();
                       services.AddTransient<IBoard, EarthBoard>();
                       services.AddTransient<IRegulator, Regulator>();
                       services.AddTransient<IStatTracker, Model.Stats.Services.StatTracker>();
                       services.AddTransient<IMainVM, MainVM>();
                       services.AddTransient<IDialogState, DialogService>();
                       services.AddTransient<IDispatcherTimer, View.Services.Timer>();
                       services.AddSingleton<App>();
                       services.AddSingleton<IAppCommander>(provider => provider.GetRequiredService<App>());
                       services.AddTransient<WebConnectionHandler>();
                       services.AddSingleton<IStatRepo>(provider => 
                       {
                           var connectionHandler = provider.GetRequiredService<WebConnectionHandler>();
                           IStatTracker statTrackerFactory() => provider.GetRequiredService<IStatTracker>();
                           var options = provider.GetRequiredService<IOptions<AppConfig>>();
                           var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                           var logger = provider.GetRequiredService<ILogger<StatRepo>>();
                           return new StatRepo(connectionHandler, statTrackerFactory, options, loggerFactory, logger);
                       });
                   })
                   .Build();
        }

        private static protected void InitializeStaticLoggers(IHost host)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            BinarySerializer.InitializeLogger(loggerFactory);

            // More here if/when needed....
        }
    }
}
