using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Model.Assets;
using Model.Core;
using Model.DataAccess;
using Model.Entities;
using Model.EventArgs;
using Shared.Interfaces;
using Shared.Interfaces.Model;
using Shared.Interfaces.View;
using Shared.Interfaces.ViewModel;
using Shared.Services.Options;
using Shared.Services.Registry;
using Shared.Services.Serializer;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using View;
using View.Services;
using ViewModel;
using Windows.ApplicationModel;

[assembly: SupportedOSPlatform("windows10.0.10240.0")]

namespace Bootstrap
{
    public class Program
    {
        public static BootStrapper? BootService { get; private set; } = null;

        [STAThread]
        static void Main()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            bool devMode = environmentName == Environments.Development;

            string appPath = DetermineAppPath();

            var appHost = BuildAppHost(devMode, environmentName, appPath, out string[] dataFileNames);
            BootService = (BootStrapper)appHost.Services.GetRequiredService<IBootStrapperService>();
            InitializeStaticLoggers(appHost);
            var app = new App(appHost, devMode, appPath, dataFileNames);
            BootService.MainApp = app;
            app.InitializeComponent();
            app.Run();

            appHost.Dispose();
        }


        public static string DetermineAppPath()
        {
            // Check if application is running as MSIX package (ie, was installed and running after public distribution)
            // If so, it's containerized and Environment.CurrentDirectory won't work.
            try {
                return Package.Current.InstalledLocation.Path;
            } catch (Exception) {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
            }
        }
        private static protected IHost BuildAppHost(bool devMode, string environmentName, string appPath, out string[] dataFileNames)
        {
            var host = Host.CreateDefaultBuilder();

            string[]? configuredDataFileNames = null;
            host.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(appPath);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true);

                var builtConfig = config.Build();

                configuredDataFileNames = builtConfig.GetSection("DataFileNames").Get<string[]>();
            });
            

            host.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                if (devMode)
                    logging.AddFilter(logLevel => logLevel >= LogLevel.Trace);
                else
                    logging.AddFilter(logLevel => logLevel >= LogLevel.Information);
            });

            host.ConfigureServices(services =>
            {
                services.Configure<AppConfig>(options => options.AppPath = appPath);
                services.AddSingleton<IRegistryInitializer, RegistryInitializer>();
                services.AddTransient<ITypeRelations, TypeRelations>();
                services.AddSingleton<ITypeRegister<ITypeRelations>, TypeRegister>();
                services.AddSingleton<IDataProvider>(serviceProvider =>
                {
                    return new DataProvider(configuredDataFileNames ?? [],
                        serviceProvider.GetRequiredService<ITypeRegister<ITypeRelations>>(),
                        serviceProvider.GetRequiredService<ILogger<DataProvider>>());
                });
                services.AddTransient<IAssetFetcher, AssetFetcher>();
                services.AddTransient<IAssetFactory, AssetFactory>();
                services.AddSingleton<IBootStrapperService>(serviceProvider =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<BootStrapper>>();
                    return new BootStrapper(logger);
                });
                services.AddTransient<IGameService, ViewModel.Services.GameService>();
                services.AddTransient<ITerritoryChangedEventArgs, TerritoryChangedEventArgs>();
                services.AddTransient<IContinentOwnerChangedEventArgs, ContinentOwnerChangedEventArgs>();
                services.AddTransient<IRuleValues, RuleValues>();
                services.AddTransient<IBoard, EarthBoard>();
                services.AddTransient<IRegulator, Regulator>();
                services.AddTransient<IGame, Game>();
                services.AddTransient<IMainVM, MainVM>();
                services.AddTransient<IDialogState, DialogService>();
                services.AddTransient<IDispatcherTimer, View.Services.Timer>();
            });

            var builtHost = host.Build();

            if (configuredDataFileNames != null) {
                dataFileNames = new string[configuredDataFileNames.Length];
                configuredDataFileNames.CopyTo(dataFileNames, 0);
            }
            else
                dataFileNames = [];

            return builtHost;
        }
        private static protected void InitializeStaticLoggers(IHost host)
        {
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            BinarySerializer.InitializeLogger(loggerFactory);

            // More here if/when needed....
        }
    }
}
